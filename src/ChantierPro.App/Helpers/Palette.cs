using ChantierPro.Core.Models;

namespace ChantierPro.Mobile.Helpers;

/// <summary>Ton sémantique : vert = conforme, orange = vigilance, rouge = critique, bleu = action / info.</summary>
public enum Tone
{
    Neutral,
    Success,
    Warning,
    Danger,
    Info
}

/// <summary>
/// Couleurs sémantiques utilisées par les ViewModels et les contrôles dessinés.
/// Les teintes sont choisies pour rester lisibles sur les thèmes clair et sombre.
/// </summary>
public static class Palette
{
    public static readonly Color Primary = Color.FromArgb("#1F3A5F");
    public static readonly Color Accent = Color.FromArgb("#F28C28");
    public static readonly Color Success = Color.FromArgb("#1F9D61");
    public static readonly Color Warning = Color.FromArgb("#D98200");
    public static readonly Color Danger = Color.FromArgb("#E04848");
    public static readonly Color Info = Color.FromArgb("#3B82F6");
    public static readonly Color Neutral = Color.FromArgb("#7C8CA6");

    public static bool IsDark => Application.Current?.RequestedTheme == AppTheme.Dark;

    public static Color TextPrimary => IsDark ? Color.FromArgb("#EAF0FA") : Color.FromArgb("#0F1B2D");
    public static Color TextSecondary => IsDark ? Color.FromArgb("#A3B3CC") : Color.FromArgb("#556580");
    public static Color Track => IsDark ? Color.FromArgb("#2D3F5E") : Color.FromArgb("#E1E8F2");
    public static Color Grid => IsDark ? Color.FromArgb("#2D3F5E") : Color.FromArgb("#DCE3EE");
    public static Color Surface => IsDark ? Color.FromArgb("#16223A") : Color.FromArgb("#FFFFFF");

    public static Color Of(Tone tone) => tone switch
    {
        Tone.Success => Success,
        Tone.Warning => Warning,
        Tone.Danger => Danger,
        Tone.Info => Info,
        _ => Neutral
    };

    /// <summary>Fond teinté translucide (fonctionne sur fond clair et sombre).</summary>
    public static Color Tint(Tone tone) => Of(tone).WithAlpha(0.18f);

    public static Tone ToneFor(AlertLevel level) => level switch
    {
        AlertLevel.Critical => Tone.Danger,
        AlertLevel.Avertissement => Tone.Warning,
        _ => Tone.Info
    };

    public static Tone ToneFor(HealthLevel level) => level switch
    {
        HealthLevel.Good => Tone.Success,
        HealthLevel.Warning => Tone.Warning,
        _ => Tone.Danger
    };

    public static Tone ToneFor(ChantierStatus status) => status switch
    {
        ChantierStatus.EnCours => Tone.Info,
        ChantierStatus.Planifie => Tone.Neutral,
        ChantierStatus.Suspendu => Tone.Warning,
        ChantierStatus.Termine => Tone.Success,
        _ => Tone.Neutral
    };

    /// <summary>Ton d'un taux de consommation budgétaire (80 % / 100 %).</summary>
    public static Tone ToneForRate(decimal rate, decimal warning = 0.80m, decimal critical = 1.00m)
        => rate >= critical ? Tone.Danger : rate >= warning ? Tone.Warning : Tone.Success;

    public static string IconFor(AlertType type) => type switch
    {
        AlertType.Budget => Icons.Payments,
        AlertType.Stock => Icons.Inventory2,
        AlertType.Retard => Icons.Schedule,
        AlertType.Incoherence => Icons.GppMaybe,
        _ => Icons.Notifications
    };

    public static string IconFor(BudgetCategory category) => category switch
    {
        BudgetCategory.Materiaux => Icons.Inventory2,
        BudgetCategory.MainOeuvre => Icons.Engineering,
        BudgetCategory.LocationEngins => Icons.Construction,
        BudgetCategory.Transport => Icons.LocalShipping,
        BudgetCategory.SousTraitance => Icons.Handyman,
        BudgetCategory.Outillage => Icons.Hardware,
        BudgetCategory.Administratif => Icons.Description,
        _ => Icons.Warning
    };

    public static string IconFor(WeatherKind weather) => weather switch
    {
        WeatherKind.Ensoleille => Icons.WbSunny,
        WeatherKind.Nuageux => Icons.Cloud,
        WeatherKind.Pluie => Icons.WaterDrop,
        WeatherKind.Orage => Icons.Bolt,
        _ => Icons.Air
    };
}
