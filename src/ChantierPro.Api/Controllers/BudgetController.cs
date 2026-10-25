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
public class BudgetController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SqlChantierRepository _repo;
    private readonly IClock _clock;
    private readonly ICurrentUserContext _user;

    public BudgetController(
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

    [HttpGet("lines/{chantierId:guid}")]
    public async Task<ActionResult<IEnumerable<BudgetLineResult>>> GetBudgetLines(Guid chantierId)
    {
        var aggregate = await _repo.GetChantierAsync(chantierId);
        if (aggregate == null) throw Errors.NotFound("Chantier");

        var lines = ChantierMetricsService.ComputeLines(aggregate, new BudgetThresholds());
        return Ok(lines);
    }

    [HttpGet("expenses/{chantierId:guid}")]
    public async Task<ActionResult<IEnumerable<Expense>>> GetExpenses(Guid chantierId)
    {
        var expenses = await _db.Expenses
            .AsNoTracking()
            .Where(e => e.ChantierId == chantierId)
            .OrderByDescending(e => e.Date)
            .ToListAsync();

        return Ok(expenses);
    }

    [HttpPost("expenses")]
    [Authorize(Roles = Roles.Contributors)]
    public async Task<ActionResult<Expense>> CreateExpense([FromBody] CreateExpenseRequest request)
    {
        if (request.Amount <= 0)
            throw Errors.Validation("Le montant de la dépense doit être supérieur à zéro.");
        if (string.IsNullOrWhiteSpace(request.Label))
            throw Errors.Validation("Le libellé de la dépense est obligatoire.");

        var expense = new Expense
        {
            Id = Guid.NewGuid(),
            ChantierId = request.ChantierId,
            Category = request.Category,
            Amount = request.Amount,
            Date = request.Date,
            Supplier = request.Supplier?.Trim() ?? string.Empty,
            Label = request.Label.Trim(),
            PaymentMode = request.PaymentMode,
            ReceiptPhotoPath = request.ReceiptPhotoPath,
            Author = _user.Name
        };

        await _repo.AddExpenseAsync(expense);
        return Created($"/api/budget/expenses/{expense.Id}", expense);
    }
}
