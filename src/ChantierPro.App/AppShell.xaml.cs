using ChantierPro.Mobile.Views;

namespace ChantierPro.Mobile;

public partial class AppShell : Shell
{
    public AppShell()
    {
        InitializeComponent();

        // Enregistrement des routes de navigation détaillées
        Routing.RegisterRoute("chantier-detail", typeof(ChantierDetailPage));
        Routing.RegisterRoute("chantier-edit", typeof(ChantierEditPage));
        Routing.RegisterRoute("phases", typeof(PhasesPage));
        Routing.RegisterRoute("phase-detail", typeof(PhaseDetailPage));
        Routing.RegisterRoute("stock-movement-edit", typeof(StockMovementEditPage));
        Routing.RegisterRoute("workers", typeof(WorkersPage));
        Routing.RegisterRoute("budget", typeof(BudgetPage));
        Routing.RegisterRoute("expense-edit", typeof(ExpenseEditPage));
        Routing.RegisterRoute("alerts", typeof(AlertsPage));
        Routing.RegisterRoute("reports", typeof(ReportsPage));
        Routing.RegisterRoute("journal", typeof(JournalPage));
    }
}
