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
public class StockController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly SqlChantierRepository _repo;
    private readonly IClock _clock;
    private readonly ICurrentUserContext _user;

    public StockController(
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

    [HttpGet("materials")]
    public async Task<ActionResult<IEnumerable<Material>>> GetMaterials()
    {
        var materials = await _repo.GetMaterialsAsync();
        return Ok(materials);
    }

    [HttpPost("materials")]
    [Authorize(Roles = Roles.Contributors)]
    public async Task<ActionResult<Material>> SaveMaterial([FromBody] Material material)
    {
        if (string.IsNullOrWhiteSpace(material.Name))
            throw Errors.Validation("Le nom du matériau est obligatoire.");

        await _repo.SaveMaterialAsync(material);
        return Ok(material);
    }

    [HttpGet("suppliers")]
    public async Task<ActionResult<IEnumerable<Supplier>>> GetSuppliers()
    {
        var suppliers = await _repo.GetSuppliersAsync();
        return Ok(suppliers);
    }

    [HttpPost("suppliers")]
    [Authorize(Roles = Roles.Contributors)]
    public async Task<ActionResult<Supplier>> SaveSupplier([FromBody] Supplier supplier)
    {
        if (string.IsNullOrWhiteSpace(supplier.Name))
            throw Errors.Validation("Le nom du fournisseur est obligatoire.");

        var existing = await _db.Suppliers.FirstOrDefaultAsync(s => s.Id == supplier.Id);
        if (existing == null) _db.Suppliers.Add(supplier);
        else
        {
            existing.Name = supplier.Name;
            existing.Phone = supplier.Phone;
            existing.Email = supplier.Email;
            existing.Address = supplier.Address;
        }
        await _db.SaveChangesAsync();
        return Ok(supplier);
    }

    [HttpGet("movements/{chantierId:guid}")]
    public async Task<ActionResult<IEnumerable<StockMovement>>> GetMovements(Guid chantierId)
    {
        var movements = await _db.StockMovements
            .AsNoTracking()
            .Where(m => m.ChantierId == chantierId)
            .OrderByDescending(m => m.Date)
            .ToListAsync();

        return Ok(movements);
    }

    [HttpPost("movements")]
    [Authorize(Roles = Roles.Contributors)]
    public async Task<ActionResult<StockMovement>> CreateMovement([FromBody] CreateMovementRequest request)
    {
        if (request.Quantity <= 0)
            throw Errors.Validation("La quantité doit être supérieure à zéro.");
        if (request.UnitPrice < 0)
            throw Errors.Validation("Le prix unitaire ne peut pas être négatif.");

        var movement = new StockMovement
        {
            Id = Guid.NewGuid(),
            ChantierId = request.ChantierId,
            MaterialId = request.MaterialId,
            Type = request.Type,
            Quantity = request.Quantity,
            UnitPrice = request.UnitPrice,
            PhaseId = request.PhaseId,
            SupplierId = request.SupplierId,
            DeliveryNoteNumber = request.DeliveryNoteNumber,
            Date = request.Date ?? _clock.Now,
            Author = _user.Name,
            IsNegativeStockAuthorized = request.IsNegativeStockAuthorized
        };

        await _repo.AddMovementAsync(movement);
        return Created($"/api/stock/movements/{movement.Id}", movement);
    }

    [HttpGet("summary/{chantierId:guid}")]
    public async Task<ActionResult<IEnumerable<object>>> GetStockSummary(Guid chantierId)
    {
        var movements = await _db.StockMovements
            .AsNoTracking()
            .Where(m => m.ChantierId == chantierId)
            .ToListAsync();

        var materials = await _db.Materials.AsNoTracking().ToListAsync();

        var result = materials.Select(m =>
        {
            var current = StockCalculator.CurrentStock(movements, chantierId, m.Id);
            var days = StockCalculator.DaysRemaining(current, movements, chantierId, m.Id, _clock.Today);
            return new
            {
                m.Id,
                m.Name,
                m.Category,
                m.Unit,
                m.UnitPrice,
                m.AlertThreshold,
                m.Barcode,
                CurrentStock = current,
                StockValue = current * m.UnitPrice,
                IsLow = current <= m.AlertThreshold,
                DaysRemaining = days
            };
        }).Where(s => s.CurrentStock > 0 || movements.Any(mov => mov.MaterialId == s.Id));

        return Ok(result);
    }
}
