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
public class ChantiersController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SqlChantierRepository _repo;
    private readonly IClock _clock;
    private readonly ICurrentUserContext _user;

    public ChantiersController(
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

    [HttpGet]
    public async Task<ActionResult<IEnumerable<object>>> GetAll([FromQuery] ChantierStatus? status, [FromQuery] string? search)
    {
        var aggregates = await _repo.GetChantiersAsync();
        var materials = await _db.Materials.AsNoTracking().ToListAsync();

        var query = aggregates.AsEnumerable();
        if (status.HasValue) query = query.Where(a => a.Chantier.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            query = query.Where(a =>
                a.Chantier.Name.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                a.Chantier.Reference.Contains(s, StringComparison.OrdinalIgnoreCase) ||
                a.Chantier.Client.Contains(s, StringComparison.OrdinalIgnoreCase));
        }

        var list = query.Select(a =>
        {
            var insight = ChantierInsightService.Analyze(a, materials, _clock.Today);
            return new
            {
                a.Chantier.Id,
                a.Chantier.Reference,
                a.Chantier.Name,
                a.Chantier.Client,
                a.Chantier.Address,
                a.Chantier.Latitude,
                a.Chantier.Longitude,
                a.Chantier.StartDate,
                a.Chantier.PlannedEndDate,
                a.Chantier.TotalBudget,
                Status = a.Chantier.Status.ToString(),
                a.Chantier.IsReadOnly,
                Progress = insight.Metrics.Progress,
                ExpectedProgress = insight.Metrics.ExpectedProgress,
                Consumed = insight.Metrics.Consumed,
                ConsumptionRate = insight.Metrics.ConsumptionRate,
                Health = insight.Metrics.Health.ToString(),
                HealthScore = insight.Metrics.HealthScore,
                AlertCount = insight.Alerts.Count
            };
        });

        return Ok(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ChantierAggregate>> GetById(Guid id)
    {
        var aggregate = await _repo.GetChantierAsync(id);
        if (aggregate == null) throw Errors.NotFound("Chantier");
        return Ok(aggregate);
    }

    [HttpGet("{id:guid}/insight")]
    public async Task<ActionResult<ChantierInsight>> GetInsight(Guid id)
    {
        var aggregate = await _repo.GetChantierAsync(id);
        if (aggregate == null) throw Errors.NotFound("Chantier");

        var materials = await _db.Materials.AsNoTracking().ToListAsync();
        var insight = ChantierInsightService.Analyze(aggregate, materials, _clock.Today);
        return Ok(insight);
    }

    [HttpGet("{id:guid}/cumulative-curve")]
    public async Task<ActionResult<IEnumerable<CurvePoint>>> GetCumulativeCurve(Guid id)
    {
        var aggregate = await _repo.GetChantierAsync(id);
        if (aggregate == null) throw Errors.NotFound("Chantier");

        var points = ChantierMetricsService.BuildCumulativeCurve(aggregate, _clock.Today);
        return Ok(points);
    }

    [HttpPost]
    [Authorize(Roles = Roles.Managers)]
    public async Task<ActionResult<ChantierAggregate>> Create([FromBody] CreateChantierRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw Errors.Validation("Le nom du chantier est obligatoire.");
        if (string.IsNullOrWhiteSpace(request.Reference))
            throw Errors.Validation("La référence du chantier est obligatoire.");
        if (request.TotalBudget <= 0)
            throw Errors.Validation("Le budget global doit être supérieur à zéro.");
        if (request.PlannedEndDate < request.StartDate)
            throw Errors.Validation("La date de fin prévue doit être postérieure à la date de début.");

        var refExists = await _db.Chantiers.AnyAsync(c => c.Reference == request.Reference.Trim());
        if (refExists)
            throw Errors.Conflict($"La référence « {request.Reference} » existe déjà.");

        var chantier = new Chantier
        {
            Id = Guid.NewGuid(),
            Reference = request.Reference.Trim(),
            Name = request.Name.Trim(),
            Client = request.Client?.Trim() ?? string.Empty,
            Address = request.Address?.Trim() ?? string.Empty,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            StartDate = request.StartDate,
            PlannedEndDate = request.PlannedEndDate,
            TotalBudget = request.TotalBudget,
            Status = request.Status,
            Description = request.Description?.Trim() ?? string.Empty
        };

        var phases = request.GenerateDefaultPhases
            ? ChantierTemplates.DefaultPhases(chantier.Id, chantier.StartDate, chantier.PlannedEndDate, chantier.TotalBudget)
            : new List<Phase>();

        var budgetLines = ChantierTemplates.DefaultBudgetLines(chantier.Id, chantier.TotalBudget);

        var agg = new ChantierAggregate
        {
            Chantier = chantier,
            Phases = phases,
            BudgetLines = budgetLines
        };

        await _repo.SaveChantierAsync(agg);
        return CreatedAtAction(nameof(GetById), new { id = chantier.Id }, agg);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<ActionResult<Chantier>> Update(Guid id, [FromBody] UpdateChantierRequest request)
    {
        var chantier = await _db.Chantiers.FirstOrDefaultAsync(c => c.Id == id);
        if (chantier == null) throw Errors.NotFound("Chantier");

        if (chantier.IsReadOnly)
            throw Errors.Locked(chantier.Name);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw Errors.Validation("Le nom du chantier est obligatoire.");
        if (string.IsNullOrWhiteSpace(request.Reference))
            throw Errors.Validation("La référence du chantier est obligatoire.");

        var refExists = await _db.Chantiers.AnyAsync(c => c.Reference == request.Reference.Trim() && c.Id != id);
        if (refExists)
            throw Errors.Conflict($"La référence « {request.Reference} » existe déjà pour un autre chantier.");

        chantier.Reference = request.Reference.Trim();
        chantier.Name = request.Name.Trim();
        chantier.Client = request.Client?.Trim() ?? string.Empty;
        chantier.Address = request.Address?.Trim() ?? string.Empty;
        chantier.Latitude = request.Latitude;
        chantier.Longitude = request.Longitude;
        chantier.StartDate = request.StartDate;
        chantier.PlannedEndDate = request.PlannedEndDate;
        chantier.TotalBudget = request.TotalBudget;
        chantier.Status = request.Status;
        chantier.Description = request.Description?.Trim() ?? string.Empty;

        await _db.SaveChangesAsync();
        await _repo.RefreshAlertsForChantierAsync(id);

        return Ok(chantier);
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<IActionResult> SetStatus(Guid id, [FromBody] SetStatusRequest request)
    {
        await _repo.SetStatusAsync(id, request.Status);
        return NoContent();
    }

    [HttpPost("{id:guid}/duplicate")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<ActionResult<ChantierAggregate>> Duplicate(Guid id)
    {
        var copy = await _repo.DuplicateChantierAsync(id);
        return CreatedAtAction(nameof(GetById), new { id = copy.Chantier.Id }, copy);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(Guid id)
    {
        var chantier = await _db.Chantiers.FirstOrDefaultAsync(c => c.Id == id);
        if (chantier == null) throw Errors.NotFound("Chantier");

        _db.Chantiers.Remove(chantier);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}
