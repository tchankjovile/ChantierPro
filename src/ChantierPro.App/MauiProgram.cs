using ChantierPro.Core.Abstractions;
using ChantierPro.Mobile.Services;
using ChantierPro.Mobile.ViewModels;
using ChantierPro.Mobile.Views;

namespace ChantierPro.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("Inter-Regular.otf", "InterRegular");
                fonts.AddFont("Inter-Medium.otf", "InterMedium");
                fonts.AddFont("Inter-SemiBold.otf", "InterSemiBold");
                fonts.AddFont("Inter-Bold.otf", "InterBold");
                fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
            });

        // Services
        builder.Services.AddSingleton<IChantierRepository, MockChantierRepository>();
        builder.Services.AddSingleton<IDialogService, DialogService>();

        // ViewModels
        builder.Services.AddTransient<LoginViewModel>();
        builder.Services.AddTransient<DashboardViewModel>();
        builder.Services.AddTransient<ChantierDetailViewModel>();
        builder.Services.AddTransient<ChantierEditViewModel>();
        builder.Services.AddTransient<PhasesViewModel>();
        builder.Services.AddTransient<PhaseDetailViewModel>();
        builder.Services.AddTransient<StockViewModel>();
        builder.Services.AddTransient<StockMovementEditViewModel>();
        builder.Services.AddTransient<AttendanceViewModel>();
        builder.Services.AddTransient<WorkersViewModel>();
        builder.Services.AddTransient<BudgetViewModel>();
        builder.Services.AddTransient<ExpenseEditViewModel>();
        builder.Services.AddTransient<AlertsViewModel>();
        builder.Services.AddTransient<ReportsViewModel>();
        builder.Services.AddTransient<JournalViewModel>();
        builder.Services.AddTransient<SettingsViewModel>();

        // Views
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<DashboardPage>();
        builder.Services.AddTransient<ChantierDetailPage>();
        builder.Services.AddTransient<ChantierEditPage>();
        builder.Services.AddTransient<PhasesPage>();
        builder.Services.AddTransient<PhaseDetailPage>();
        builder.Services.AddTransient<StockPage>();
        builder.Services.AddTransient<StockMovementEditPage>();
        builder.Services.AddTransient<AttendancePage>();
        builder.Services.AddTransient<WorkersPage>();
        builder.Services.AddTransient<BudgetPage>();
        builder.Services.AddTransient<ExpenseEditPage>();
        builder.Services.AddTransient<AlertsPage>();
        builder.Services.AddTransient<ReportsPage>();
        builder.Services.AddTransient<JournalPage>();
        builder.Services.AddTransient<SettingsPage>();

        return builder.Build();
    }
}
