using ChantierPro.Api.Dtos;
using ChantierPro.Api.Infrastructure;
using ChantierPro.Api.Services;
using ChantierPro.Core.Models;
using ChantierPro.Core.Services;
using ChantierPro.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChantierPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PhasesController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SqlChantierRepository _repo;
    private readonly IClock _clock;
    private readonly ICurrentUserContext _user;

    public PhasesController(
        AppDbContext db,
        SqlChantierRepository repo,
        IClock clock,
        ICurrentUserContext user)
    {
        _db = db;
        _repo = repo;
        _clock = clock;
        _user = user;
    }

    [HttpGet("by-chantier/{chantierId:guid}")]
    public async Task<ActionResult<IEnumerable<Phase>>> GetByChantier(Guid chantierId)
    {
        var phases = await _db.Phases
            .AsNoTracking()
            .Where(p => p.ChantierId == chantierId)
            .OrderBy(p => p.Order)
            .Include(p => p.Tasks.OrderBy(t => t.Order))
            .Include(p => p.History.OrderByDescending(h => h.Date))
            .ToListAsync();

        return Ok(phases);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<Phase>> GetById(Guid id)
    {
        var phase = await _db.Phases
            .AsNoTracking()
            .Include(p => p.Tasks.OrderBy(t => t.Order))
            .Include(p => p.History.OrderByDescending(h => h.Date))
            .FirstOrDefaultAsync(p => p.Id == id);

        if (phase == null) throw Errors.NotFound("Phase");
        return Ok(phase);
    }

    [HttpPost("by-chantier/{chantierId:guid}")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<ActionResult<Phase>> Create(Guid chantierId, [FromBody] CreatePhaseRequest request)
    {
        var chantier = await _db.Chantiers.FirstOrDefaultAsync(c => c.Id == chantierId);
        if (chantier == null) throw Errors.NotFound("Chantier");
        if (chantier.IsReadOnly) throw Errors.Locked(chantier.Name);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw Errors.Validation("Le nom de la phase est obligatoire.");
        if (request.Weight < 0 || request.Weight > 100)
            throw Errors.Validation("Le poids doit être compris entre 0 et 100 %.");

        var phase = new Phase
        {
            Id = Guid.NewGuid(),
            ChantierId = chantierId,
            Name = request.Name.Trim(),
            Order = request.Order,
            Weight = request.Weight,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            AllocatedBudget = request.AllocatedBudget,
            Progress = 0m
        };

        _db.Phases.Add(phase);
        await _db.SaveChangesAsync();
        await _repo.RefreshAlertsForChantierAsync(chantierId);

        return CreatedAtAction(nameof(GetById), new { id = phase.Id }, phase);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<ActionResult<Phase>> UpdateDetails(Guid id, [FromBody] UpdatePhaseDetailsRequest request)
    {
        var phase = await _db.Phases.FirstOrDefaultAsync(p => p.Id == id);
        if (phase == null) throw Errors.NotFound("Phase");

        var chantier = await _db.Chantiers.FirstOrDefaultAsync(c => c.Id == phase.ChantierId);
        if (chantier == null) throw Errors.NotFound("Chantier");
        if (chantier.IsReadOnly) throw Errors.Locked(chantier.Name);

        phase.Name = request.Name.Trim();
        phase.Order = request.Order;
        phase.Weight = request.Weight;
        phase.StartDate = request.StartDate;
        phase.EndDate = request.EndDate;
        phase.AllocatedBudget = request.AllocatedBudget;

        await _db.SaveChangesAsync();
        await _repo.RefreshAlertsForChantierAsync(phase.ChantierId);

        return Ok(phase);
    }

    [HttpPost("{id:guid}/progress")]
    [Authorize(Roles = Roles.Contributors)]
    public async Task<ActionResult<Phase>> SaveProgress(Guid id, [FromBody] SavePhaseRequest request)
    {
        var phase = await _db.Phases.FirstOrDefaultAsync(p => p.Id == id);
        if (phase == null) throw Errors.NotFound("Phase");

        phase.Progress = request.Progress;
        await _repo.SavePhaseAsync(phase, _user.Name, request.Note ?? string.Empty);

        return await GetById(id);
    }

    [HttpPost("{id:guid}/tasks")]
    [Authorize(Roles = Roles.Contributors)]
    public async Task<ActionResult<PhaseTask>> AddTask(Guid id, [FromBody] CreateTaskRequest request)
    {
        var phase = await _db.Phases.FirstOrDefaultAsync(p => p.Id == id);
        if (phase == null) throw Errors.NotFound("Phase");

        var chantier = await _db.Chantiers.FirstOrDefaultAsync(c => c.Id == phase.ChantierId);
        if (chantier == null) throw Errors.NotFound("Chantier");
        if (chantier.IsReadOnly) throw Errors.Locked(chantier.Name);

        var task = new PhaseTask
        {
            Id = Guid.NewGuid(),
            PhaseId = id,
            Label = request.Label.Trim(),
            IsDone = false,
            Order = request.Order
        };

        _db.PhaseTasks.Add(task);
        await _db.SaveChangesAsync();

        return Created($"/api/phases/{id}/tasks/{task.Id}", task);
    }

    [HttpPut("tasks/{taskId:guid}")]
    [Authorize(Roles = Roles.Contributors)]
    public async Task<ActionResult<PhaseTask>> UpdateTask(Guid taskId, [FromBody] UpdateTaskRequest request)
    {
        var task = await _db.PhaseTasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null) throw Errors.NotFound("Tâche");

        var phase = await _db.Phases.FirstOrDefaultAsync(p => p.Id == task.PhaseId);
        if (phase != null)
        {
            var chantier = await _db.Chantiers.FirstOrDefaultAsync(c => c.Id == phase.ChantierId);
            if (chantier != null && chantier.IsReadOnly) throw Errors.Locked(chantier.Name);
        }

        task.Label = request.Label.Trim();
        task.IsDone = request.IsDone;
        task.Order = request.Order;

        await _db.SaveChangesAsync();
        return Ok(task);
    }

    [HttpDelete("tasks/{taskId:guid}")]
    [Authorize(Roles = Roles.Contributors)]
    public async Task<IActionResult> DeleteTask(Guid taskId)
    {
        var task = await _db.PhaseTasks.FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null) throw Errors.NotFound("Tâche");

        var phase = await _db.Phases.FirstOrDefaultAsync(p => p.Id == task.PhaseId);
        if (phase != null)
        {
            var chantier = await _db.Chantiers.FirstOrDefaultAsync(c => c.Id == phase.ChantierId);
            if (chantier != null && chantier.IsReadOnly) throw Errors.Locked(chantier.Name);
        }

        _db.PhaseTasks.Remove(task);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
