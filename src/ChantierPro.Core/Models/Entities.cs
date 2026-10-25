namespace ChantierPro.Core.Models;

/// <summary>Utilisateur de l'application.</summary>
public class AppUser
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}

public class Chantier
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Reference { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public DateTime StartDate { get; set; } = DateTime.Today;
    public DateTime PlannedEndDate { get; set; } = DateTime.Today.AddMonths(6);
    public decimal TotalBudget { get; set; }
    public ChantierStatus Status { get; set; } = ChantierStatus.Planifie;
    public string Description { get; set; } = string.Empty;

    /// <summary>RG-07 : un chantier clôturé est en lecture seule.</summary>
    public bool IsReadOnly => Status == ChantierStatus.Cloture;
}

public class Phase
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChantierId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; }

    /// <summary>Poids dans l'avancement global, en % (la somme des poids vaut 100).</summary>
    public decimal Weight { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal AllocatedBudget { get; set; }

    /// <summary>Avancement de la phase, de 0 à 100 %.</summary>
    public decimal Progress { get; set; }
    public List<PhaseTask> Tasks { get; set; } = new();
    public List<ProgressEntry> History { get; set; } = new();
}

public class PhaseTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PhaseId { get; set; }
    public string Label { get; set; } = string.Empty;
    public bool IsDone { get; set; }

    /// <summary>Position de la tâche dans la checklist de la phase (tri stable côté serveur).</summary>
    public int Order { get; set; }
}

/// <summary>Historique d'une mise à jour d'avancement (F-PHA-03, RG-06).</summary>
public class ProgressEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid PhaseId { get; set; }
    public decimal Progress { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public string Author { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}

public class Material
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;

    /// <summary>Prix unitaire moyen pondéré (RG-08).</summary>
    public decimal UnitPrice { get; set; }
    public decimal AlertThreshold { get; set; }
    public string? Barcode { get; set; }
}

public class Supplier
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
}

public class StockMovement
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChantierId { get; set; }
    public Guid MaterialId { get; set; }
    public MovementType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public Guid? PhaseId { get; set; }
    public Guid? SupplierId { get; set; }
    public string? DeliveryNoteNumber { get; set; }
    public DateTime Date { get; set; } = DateTime.Now;
    public string Author { get; set; } = string.Empty;
    public bool IsNegativeStockAuthorized { get; set; }

    public decimal TotalCost => Quantity * UnitPrice;
}

public class Worker
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Trade { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal DailyRate { get; set; }
    public WorkerStatus Status { get; set; } = WorkerStatus.Actif;
}

public class Attendance
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid WorkerId { get; set; }
    public Guid ChantierId { get; set; }
    public Guid? PhaseId { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public AttendanceStatus Status { get; set; }
    public decimal OvertimeHours { get; set; }

    /// <summary>Taux journalier de l'ouvrier au moment du pointage (traçabilité du coût).</summary>
    public decimal DailyRate { get; set; }

    /// <summary>RG-05 : coût de main-d'œuvre de la journée.</summary>
    public decimal Cost => Services.LabourCostCalculator.DayCost(DailyRate, Status, OvertimeHours);
}

/// <summary>Poste budgétaire prévisionnel (F-DEP-01).</summary>
public class BudgetLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChantierId { get; set; }
    public BudgetCategory Category { get; set; }
    public decimal PlannedAmount { get; set; }
}

public class Expense
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChantierId { get; set; }
    public BudgetCategory Category { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public string Supplier { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public PaymentMode PaymentMode { get; set; }
    public string? ReceiptPhotoPath { get; set; }
    public string Author { get; set; } = string.Empty;
}

public class AlertItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChantierId { get; set; }
    public AlertType Type { get; set; }
    public AlertLevel Level { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime Date { get; set; } = DateTime.Now;
    public bool IsRead { get; set; }

    /// <summary>Clé fonctionnelle permettant d'éviter les doublons lors de la régénération.</summary>
    public string Key { get; set; } = string.Empty;
}

public class JournalEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChantierId { get; set; }
    public DateTime Date { get; set; } = DateTime.Today;
    public WeatherKind Weather { get; set; }
    public string Note { get; set; } = string.Empty;
    public string Incident { get; set; } = string.Empty;
    public string Visitors { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
}

public class SitePhoto
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ChantierId { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Stage { get; set; } = "Pendant";
    public DateTime Date { get; set; } = DateTime.Now;
    public string Author { get; set; } = string.Empty;
}

/// <summary>Agrégat regroupant toutes les données d'un chantier pour les calculs métier.</summary>
public class ChantierAggregate
{
    public Chantier Chantier { get; set; } = new();
    public List<Phase> Phases { get; set; } = new();
    public List<BudgetLine> BudgetLines { get; set; } = new();
    public List<Expense> Expenses { get; set; } = new();
    public List<StockMovement> Movements { get; set; } = new();
    public List<Attendance> Attendances { get; set; } = new();
    public List<JournalEntry> Journal { get; set; } = new();
    public List<SitePhoto> Photos { get; set; } = new();
}
