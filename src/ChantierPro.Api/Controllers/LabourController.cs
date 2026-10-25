using ChantierPro.Api.Dtos;
using ChantierPro.Api.Infrastructure;
using ChantierPro.Api.Services;
using ChantierPro.Core.Models;
using ChantierPro.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChantierPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class LabourController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SqlChantierRepository _repo;
    private readonly IClock _clock;
    private readonly ICurrentUserContext _user;

    public LabourController(
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

    [HttpGet("workers")]
    public async Task<ActionResult<IEnumerable<Worker>>> GetWorkers([FromQuery] WorkerStatus? status)
    {
        var query = _db.Workers.AsNoTracking();
        if (status.HasValue) query = query.Where(w => w.Status == status.Value);
        var workers = await query.OrderBy(w => w.Name).ToListAsync();
        return Ok(workers);
    }

    [HttpPost("workers")]
    [Authorize(Roles = Roles.Managers)]
    public async Task<ActionResult<Worker>> SaveWorker([FromBody] Worker worker)
    {
        if (string.IsNullOrWhiteSpace(worker.Name))
            throw Errors.Validation("Le nom de l'ouvrier est obligatoire.");
        if (worker.DailyRate < 0)
            throw Errors.Validation("Le taux journalier ne peut pas être négatif.");

        await _repo.SaveWorkerAsync(worker);
        return Ok(worker);
    }

    [HttpGet("attendances/{chantierId:guid}")]
    public async Task<ActionResult<IEnumerable<object>>> GetAttendances(Guid chantierId, [FromQuery] DateTime? date)
    {
        var targetDate = (date ?? _clock.Today).Date;
        var attendances = await _db.Attendances
            .AsNoTracking()
            .Where(a => a.ChantierId == chantierId && a.Date.Date == targetDate)
            .ToListAsync();

        var workers = await _db.Workers.AsNoTracking().ToDictionaryAsync(w => w.Id, w => w);

        var result = attendances.Select(a =>
        {
            workers.TryGetValue(a.WorkerId, out var worker);
            return new
            {
                a.Id,
                a.ChantierId,
                a.WorkerId,
                WorkerName = worker?.Name ?? "Inconnu",
                WorkerTrade = worker?.Trade ?? string.Empty,
                a.PhaseId,
                a.Date,
                Status = a.Status.ToString(),
                a.OvertimeHours,
                a.DailyRate,
                Cost = a.Cost
            };
        });

        return Ok(result);
    }

    [HttpPost("attendances/{chantierId:guid}")]
    [Authorize(Roles = Roles.Contributors)]
    public async Task<IActionResult> SaveAttendances(Guid chantierId, [FromBody] SaveAttendancesRequest request)
    {
        var workers = await _db.Workers.AsNoTracking().ToDictionaryAsync(w => w.Id, w => w.DailyRate);

        var list = new List<Attendance>();
        foreach (var item in request.Attendances)
        {
            if (!workers.TryGetValue(item.WorkerId, out var dailyRate))
            {
                throw Errors.Validation($"L'ouvrier {item.WorkerId} est introuvable.");
            }

            list.Add(new Attendance
            {
                Id = Guid.NewGuid(),
                ChantierId = chantierId,
                WorkerId = item.WorkerId,
                PhaseId = item.PhaseId,
                Date = request.Date.Date,
                Status = item.Status,
                OvertimeHours = item.OvertimeHours,
                DailyRate = dailyRate
            });
        }

        await _repo.SaveAttendancesAsync(chantierId, request.Date, list);
        return NoContent();
    }
}
