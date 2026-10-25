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
public class AlertsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SqlChantierRepository _repo;

    public AlertsController(AppDbContext db, SqlChantierRepository repo)
    {
        _db = db;
        _repo = repo;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<AlertItem>>> GetAlerts([FromQuery] Guid? chantierId, [FromQuery] bool? unreadOnly)
    {
        var query = _db.Alerts.AsNoTracking();
        if (chantierId.HasValue) query = query.Where(a => a.ChantierId == chantierId.Value);
        if (unreadOnly == true) query = query.Where(a => !a.IsRead);

        var list = await query
            .OrderByDescending(a => a.Level)
            .ThenByDescending(a => a.Date)
            .ToListAsync();

        return Ok(list);
    }

    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> MarkRead(Guid id, [FromQuery] bool isRead = true)
    {
        await _repo.MarkAlertReadAsync(id, isRead);
        return NoContent();
    }

    [HttpPost("mark-all-read")]
    public async Task<IActionResult> MarkAllRead()
    {
        await _repo.MarkAllAlertsReadAsync();
        return NoContent();
    }
}
