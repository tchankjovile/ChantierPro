using ChantierPro.Core.Abstractions;
using ChantierPro.Core.Models;
using ChantierPro.Core.Services;
using ChantierPro.Data;
using ChantierPro.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ChantierPro.Api.Services;

/// <summary>
/// Implémentation complète de IChantierRepository branchée directement sur la base de données via AppDbContext.
/// Réutilisable côté serveur API ou directement dans l'application si nécessaire.
/// </summary>
public class SqlChantierRepository : IChantierRepository
{
    private readonly AppDbContext _db;
    private readonly Infrastructure.ICurrentUserContext _userContext;
    private readonly Infrastructure.IClock _clock;

    public SqlChantierRepository(
        AppDbContext db,
        Infrastructure.ICurrentUserContext userContext,
        Infrastructure.IClock clock)
    {
        _db = db;
        _userContext = userContext;
        _clock = clock;
    }

    public AppUser CurrentUser => new()
    {
        Id = _userContext.UserId ?? Guid.Empty,
        Name = _userContext.Name,
        Email = _userContext.Email,
        Role = _userContext.Role ?? UserRole.Consultant
    };

    public async Task<IReadOnlyList<ChantierAggregate>> GetChantiersAsync()
    {
        var chantiers = await _db.Chantiers.AsNoTracking().ToListAsync();
        var result = new List<ChantierAggregate>();
        foreach (var c in chantiers)
        {
            var agg = await LoadAggregateAsync(c.Id);
            if (agg != null) result.Add(agg);
        }
        return result;
    }

    public async Task<ChantierAggregate?> GetChantierAsync(Guid id)
    {
        return await LoadAggregateAsync(id);
    }

    public async Task<ChantierAggregate?> LoadAggregateAsync(Guid id)
    {
        var chantier = await _db.Chantiers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == id);
        if (chantier == null) return null;

        var phases = await _db.Phases
            .AsNoTracking()
            .Where(p => p.ChantierId == id)
            .OrderBy(p => p.Order)
            .Include(p => p.Tasks.OrderBy(t => t.Order))
            .Include(p => p.History.OrderByDescending(h => h.Date))
            .ToListAsync();

        var budgetLines = await _db.BudgetLines
            .AsNoTracking()
            .Where(b => b.ChantierId == id)
            .ToListAsync();

        var expenses = await _db.Expenses
            .AsNoTracking()
            .Where(e => e.ChantierId == id)
            .OrderByDescending(e => e.Date)
            .ToListAsync();

        var movements = await _db.StockMovements
            .AsNoTracking()
            .Where(m => m.ChantierId == id)
            .OrderByDescending(m => m.Date)
            .ToListAsync();

        var attendances = await _db.Attendances
            .AsNoTracking()
            .Where(a => a.ChantierId == id)
            .OrderByDescending(a => a.Date)
            .ToListAsync();

        var journal = await _db.JournalEntries
            .AsNoTracking()
            .Where(j => j.ChantierId == id)
            .OrderByDescending(j => j.Date)
            .ToListAsync();

        var photos = await _db.Photos
            .AsNoTracking()
            .Where(p => p.ChantierId == id)
            .OrderByDescending(p => p.Date)
            .ToListAsync();

        return new ChantierAggregate
        {
            Chantier = chantier,
            Phases = phases,
            BudgetLines = budgetLines,
            Expenses = expenses,
            Movements = movements,
            Attendances = attendances,
            Journal = journal,
            Photos = photos
        };
    }

    public async Task SaveChantierAsync(ChantierAggregate data)
    {
        var exists = await _db.Chantiers.AnyAsync(c => c.Id == data.Chantier.Id);
        if (!exists)
        {
            _db.Chantiers.Add(data.Chantier);
            if (data.Phases.Count > 0) _db.Phases.AddRange(data.Phases);
            if (data.BudgetLines.Count > 0) _db.BudgetLines.AddRange(data.BudgetLines);
            if (data.Expenses.Count > 0) _db.Expenses.AddRange(data.Expenses);
            if (data.Movements.Count > 0) _db.StockMovements.AddRange(data.Movements);
            if (data.Attendances.Count > 0) _db.Attendances.AddRange(data.Attendances);
            if (data.Journal.Count > 0) _db.JournalEntries.AddRange(data.Journal);
            if (data.Photos.Count > 0) _db.Photos.AddRange(data.Photos);
        }
        else
        {
            _db.Chantiers.Update(data.Chantier);
        }

        await _db.SaveChangesAsync();
        await RefreshAlertsForChantierAsync(data.Chantier.Id);
    }

    public async Task<ChantierAggregate> DuplicateChantierAsync(Guid id)
    {
        var orig = await LoadAggregateAsync(id)
                   ?? throw Infrastructure.Errors.NotFound("Chantier");

        var newChantier = new Chantier
        {
            Id = Guid.NewGuid(),
            Reference = orig.Chantier.Reference + "-COPIE",
            Name = orig.Chantier.Name + " (Copie)",
            Client = orig.Chantier.Client,
            Address = orig.Chantier.Address,
            Latitude = orig.Chantier.Latitude,
            Longitude = orig.Chantier.Longitude,
            StartDate = _clock.Today,
            PlannedEndDate = _clock.Today.AddMonths(6),
            TotalBudget = orig.Chantier.TotalBudget,
            Status = ChantierStatus.Planifie,
            Description = orig.Chantier.Description
        };

        var newPhases = ChantierTemplates.DefaultPhases(
            newChantier.Id,
            newChantier.StartDate,
            newChantier.PlannedEndDate,
            newChantier.TotalBudget);

        var newBudget = ChantierTemplates.DefaultBudgetLines(
            newChantier.Id,
            newChantier.TotalBudget);

        var copy = new ChantierAggregate
        {
            Chantier = newChantier,
            Phases = newPhases,
            BudgetLines = newBudget
        };

        await SaveChantierAsync(copy);
        return copy;
    }

    public async Task SetStatusAsync(Guid id, ChantierStatus status)
    {
        var chantier = await _db.Chantiers.FirstOrDefaultAsync(c => c.Id == id)
                       ?? throw Infrastructure.Errors.NotFound("Chantier");

        chantier.Status = status;
        await _db.SaveChangesAsync();
    }

    public async Task SavePhaseAsync(Phase phase, string author, string note)
    {
        var currentPhase = await _db.Phases.FirstOrDefaultAsync(p => p.Id == phase.Id)
                           ?? throw Infrastructure.Errors.NotFound("Phase");

        var chantier = await _db.Chantiers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == currentPhase.ChantierId)
                       ?? throw Infrastructure.Errors.NotFound("Chantier");

        if (chantier.IsReadOnly)
            throw Infrastructure.Errors.Locked(chantier.Name);

        currentPhase.Progress = ProgressCalculator.Clamp(phase.Progress);

        var entry = new ProgressEntry
        {
            Id = Guid.NewGuid(),
            PhaseId = currentPhase.Id,
            Progress = currentPhase.Progress,
            Date = _clock.Now,
            Author = string.IsNullOrWhiteSpace(author) ? _userContext.Name : author,
            Note = note ?? string.Empty
        };

        _db.ProgressEntries.Add(entry);
        await _db.SaveChangesAsync();
        await RefreshAlertsForChantierAsync(currentPhase.ChantierId);
    }

    public async Task<IReadOnlyList<Material>> GetMaterialsAsync()
    {
        return await _db.Materials.AsNoTracking().OrderBy(m => m.Name).ToListAsync();
    }

    public async Task SaveMaterialAsync(Material material)
    {
        var existing = await _db.Materials.FirstOrDefaultAsync(m => m.Id == material.Id);
        if (existing == null)
        {
            _db.Materials.Add(material);
        }
        else
        {
            existing.Name = material.Name;
            existing.Category = material.Category;
            existing.Unit = material.Unit;
            existing.UnitPrice = material.UnitPrice;
            existing.AlertThreshold = material.AlertThreshold;
            existing.Barcode = material.Barcode;
        }

        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<Supplier>> GetSuppliersAsync()
    {
        return await _db.Suppliers.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
    }

    public async Task AddMovementAsync(StockMovement movement)
    {
        var chantier = await _db.Chantiers.FirstOrDefaultAsync(c => c.Id == movement.ChantierId)
                       ?? throw Infrastructure.Errors.NotFound("Chantier");

        if (chantier.IsReadOnly)
            throw Infrastructure.Errors.Locked(chantier.Name);

        var material = await _db.Materials.FirstOrDefaultAsync(m => m.Id == movement.MaterialId)
                       ?? throw Infrastructure.Errors.NotFound("Matériau");

        // Calculer le stock actuel sur le chantier
        var pastMovements = await _db.StockMovements
            .AsNoTracking()
            .Where(m => m.ChantierId == movement.ChantierId && m.MaterialId == movement.MaterialId)
            .ToListAsync();

        var currentStock = StockCalculator.CurrentStock(pastMovements, movement.ChantierId, movement.MaterialId);

        if (movement.Type == MovementType.Sortie)
        {
            // RG-03 : contrôle anti-stock négatif
            if (!StockCalculator.CanIssue(currentStock, movement.Quantity, movement.IsNegativeStockAuthorized))
            {
                throw Infrastructure.Errors.Conflict(
                    $"Stock insuffisant pour {material.Name} (disponible : {currentStock} {material.Unit}, demandé : {movement.Quantity}). Cochez l'autorisation exceptionnelle si nécessaire.");
            }
        }
        else if (movement.Type == MovementType.Entree)
        {
            // RG-08 : actualisation du PUMP (Prix Unitaire Moyen Pondéré)
            material.UnitPrice = StockCalculator.WeightedAveragePrice(
                Math.Max(0m, currentStock),
                material.UnitPrice,
                movement.Quantity,
                movement.UnitPrice);
        }

        movement.Author = string.IsNullOrWhiteSpace(movement.Author) ? _userContext.Name : movement.Author;
        if (movement.Date == default) movement.Date = _clock.Now;

        _db.StockMovements.Add(movement);
        await _db.SaveChangesAsync();
        await RefreshAlertsForChantierAsync(movement.ChantierId);
    }

    public async Task<IReadOnlyList<Worker>> GetWorkersAsync()
    {
        return await _db.Workers.AsNoTracking().OrderBy(w => w.Name).ToListAsync();
    }

    public async Task SaveWorkerAsync(Worker worker)
    {
        var existing = await _db.Workers.FirstOrDefaultAsync(w => w.Id == worker.Id);
        if (existing == null)
        {
            _db.Workers.Add(worker);
        }
        else
        {
            existing.Name = worker.Name;
            existing.Trade = worker.Trade;
            existing.Phone = worker.Phone;
            existing.DailyRate = worker.DailyRate;
            existing.Status = worker.Status;
        }

        await _db.SaveChangesAsync();
    }

    public async Task SaveAttendancesAsync(Guid chantierId, DateTime date, IEnumerable<Attendance> attendances)
    {
        var chantier = await _db.Chantiers.FirstOrDefaultAsync(c => c.Id == chantierId)
                       ?? throw Infrastructure.Errors.NotFound("Chantier");

        if (chantier.IsReadOnly)
            throw Infrastructure.Errors.Locked(chantier.Name);

        var dateOnly = date.Date;
        var existing = await _db.Attendances
            .Where(a => a.ChantierId == chantierId && a.Date.Date == dateOnly)
            .ToListAsync();

        _db.Attendances.RemoveRange(existing);

        foreach (var att in attendances)
        {
            att.ChantierId = chantierId;
            att.Date = dateOnly;
            _db.Attendances.Add(att);
        }

        await _db.SaveChangesAsync();
        await RefreshAlertsForChantierAsync(chantierId);
    }

    public async Task AddExpenseAsync(Expense expense)
    {
        var chantier = await _db.Chantiers.FirstOrDefaultAsync(c => c.Id == expense.ChantierId)
                       ?? throw Infrastructure.Errors.NotFound("Chantier");

        if (chantier.IsReadOnly)
            throw Infrastructure.Errors.Locked(chantier.Name);

        expense.Author = string.IsNullOrWhiteSpace(expense.Author) ? _userContext.Name : expense.Author;
        if (expense.Date == default) expense.Date = _clock.Today;

        _db.Expenses.Add(expense);
        await _db.SaveChangesAsync();
        await RefreshAlertsForChantierAsync(expense.ChantierId);
    }

    public async Task AddJournalEntryAsync(JournalEntry entry)
    {
        var chantier = await _db.Chantiers.FirstOrDefaultAsync(c => c.Id == entry.ChantierId)
                       ?? throw Infrastructure.Errors.NotFound("Chantier");

        if (chantier.IsReadOnly)
            throw Infrastructure.Errors.Locked(chantier.Name);

        entry.Author = string.IsNullOrWhiteSpace(entry.Author) ? _userContext.Name : entry.Author;
        if (entry.Date == default) entry.Date = _clock.Today;

        _db.JournalEntries.Add(entry);
        await _db.SaveChangesAsync();
    }

    public async Task AddPhotoAsync(SitePhoto photo)
    {
        var chantier = await _db.Chantiers.FirstOrDefaultAsync(c => c.Id == photo.ChantierId)
                       ?? throw Infrastructure.Errors.NotFound("Chantier");

        if (chantier.IsReadOnly)
            throw Infrastructure.Errors.Locked(chantier.Name);

        photo.Author = string.IsNullOrWhiteSpace(photo.Author) ? _userContext.Name : photo.Author;
        if (photo.Date == default) photo.Date = _clock.Now;

        _db.Photos.Add(photo);
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<AlertItem>> GetAlertsAsync()
    {
        return await _db.Alerts
            .AsNoTracking()
            .OrderByDescending(a => a.Level)
            .ThenByDescending(a => a.Date)
            .ToListAsync();
    }

    public async Task ReplaceAlertsAsync(Guid chantierId, IEnumerable<AlertItem> generated)
    {
        var existing = await _db.Alerts.Where(a => a.ChantierId == chantierId).ToListAsync();
        _db.Alerts.RemoveRange(existing);
        _db.Alerts.AddRange(generated);
        await _db.SaveChangesAsync();
    }

    public async Task MarkAlertReadAsync(Guid alertId, bool isRead)
    {
        var a = await _db.Alerts.FirstOrDefaultAsync(x => x.Id == alertId);
        if (a != null)
        {
            a.IsRead = isRead;
            await _db.SaveChangesAsync();
        }
    }

    public async Task MarkAllAlertsReadAsync()
    {
        var alerts = await _db.Alerts.Where(a => !a.IsRead).ToListAsync();
        foreach (var a in alerts) a.IsRead = true;
        await _db.SaveChangesAsync();
    }

    public async Task RefreshAlertsForChantierAsync(Guid chantierId)
    {
        var aggregate = await LoadAggregateAsync(chantierId);
        if (aggregate == null) return;

        var materials = await _db.Materials.AsNoTracking().ToListAsync();
        var insight = ChantierInsightService.Analyze(aggregate, materials, _clock.Today);

        // Mise à jour des alertes en conservant l'état IsRead pour les alertes de même clé
        var oldAlerts = await _db.Alerts.Where(a => a.ChantierId == chantierId).ToListAsync();
        var readKeys = oldAlerts.Where(a => a.IsRead).Select(a => a.Key).ToHashSet();

        _db.Alerts.RemoveRange(oldAlerts);

        foreach (var alert in insight.Alerts)
        {
            if (readKeys.Contains(alert.Key))
            {
                alert.IsRead = true;
            }
            _db.Alerts.Add(alert);
        }

        await _db.SaveChangesAsync();
    }
}
