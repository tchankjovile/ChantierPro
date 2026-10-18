using ChantierPro.Core.Models;

namespace ChantierPro.Core.Abstractions;

/// <summary>
/// Accès aux données de l'application. Cette interface sera implémentée par la couche
/// ChantierPro.Data (SQLite + file de synchronisation) lors de la partie « backend ».
/// Le frontend s'appuie dessus via une implémentation en mémoire alimentée de données de démonstration.
/// </summary>
public interface IChantierRepository
{
    // Chantiers
    Task<IReadOnlyList<ChantierAggregate>> GetChantiersAsync();
    Task<ChantierAggregate?> GetChantierAsync(Guid id);
    Task SaveChantierAsync(ChantierAggregate data);
    Task<ChantierAggregate> DuplicateChantierAsync(Guid id);
    Task SetStatusAsync(Guid id, ChantierStatus status);

    // Phases
    Task SavePhaseAsync(Phase phase, string author, string note);

    // Matériaux et fournisseurs
    Task<IReadOnlyList<Material>> GetMaterialsAsync();
    Task SaveMaterialAsync(Material material);
    Task<IReadOnlyList<Supplier>> GetSuppliersAsync();
    Task AddMovementAsync(StockMovement movement);

    // Main-d'œuvre
    Task<IReadOnlyList<Worker>> GetWorkersAsync();
    Task SaveWorkerAsync(Worker worker);
    Task SaveAttendancesAsync(Guid chantierId, DateTime date, IEnumerable<Attendance> attendances);

    // Dépenses
    Task AddExpenseAsync(Expense expense);

    // Journal et photos
    Task AddJournalEntryAsync(JournalEntry entry);
    Task AddPhotoAsync(SitePhoto photo);

    // Alertes
    Task<IReadOnlyList<AlertItem>> GetAlertsAsync();
    Task ReplaceAlertsAsync(Guid chantierId, IEnumerable<AlertItem> generated);
    Task MarkAlertReadAsync(Guid alertId, bool isRead);
    Task MarkAllAlertsReadAsync();

    // Utilisateur courant
    AppUser CurrentUser { get; }
}
