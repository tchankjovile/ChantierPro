using System.Globalization;
using ChantierPro.Core.Models;

namespace ChantierPro.Core.Services;

/// <summary>Libellés français des énumérations (langue par défaut, F-PAR-04).</summary>
public static class Labels
{
    public static string Of(ChantierStatus s) => s switch
    {
        ChantierStatus.Planifie => "Planifié",
        ChantierStatus.EnCours => "En cours",
        ChantierStatus.Suspendu => "Suspendu",
        ChantierStatus.Termine => "Terminé",
        ChantierStatus.Cloture => "Clôturé",
        _ => s.ToString()
    };

    public static string Of(UserRole r) => r switch
    {
        UserRole.Administrateur => "Administrateur",
        UserRole.ChefChantier => "Chef de chantier",
        UserRole.Magasinier => "Magasinier",
        UserRole.Consultant => "Consultant",
        _ => r.ToString()
    };

    public static string Of(BudgetCategory c) => c switch
    {
        BudgetCategory.Materiaux => "Matériaux",
        BudgetCategory.MainOeuvre => "Main-d'œuvre",
        BudgetCategory.LocationEngins => "Location d'engins",
        BudgetCategory.Transport => "Transport",
        BudgetCategory.SousTraitance => "Sous-traitance",
        BudgetCategory.Outillage => "Outillage",
        BudgetCategory.Administratif => "Administratif",
        BudgetCategory.Imprevus => "Imprévus",
        _ => c.ToString()
    };

    public static string Of(PaymentMode p) => p switch
    {
        PaymentMode.Especes => "Espèces",
        PaymentMode.MobileMoney => "Mobile Money",
        PaymentMode.Virement => "Virement",
        PaymentMode.Cheque => "Chèque",
        PaymentMode.Carte => "Carte",
        _ => p.ToString()
    };

    public static string Of(AttendanceStatus s) => s switch
    {
        AttendanceStatus.Present => "Présent",
        AttendanceStatus.Absent => "Absent",
        AttendanceStatus.Retard => "Retard",
        AttendanceStatus.DemiJournee => "Demi-journée",
        _ => s.ToString()
    };

    public static string Of(AlertLevel l) => l switch
    {
        AlertLevel.Info => "Information",
        AlertLevel.Avertissement => "Avertissement",
        AlertLevel.Critical => "Critique",
        _ => l.ToString()
    };

    public static string Of(AlertType t) => t switch
    {
        AlertType.Budget => "Budget",
        AlertType.Stock => "Stock",
        AlertType.Retard => "Retard",
        AlertType.Incoherence => "Incohérence",
        AlertType.Pointage => "Pointage",
        _ => t.ToString()
    };

    public static string Of(WeatherKind w) => w switch
    {
        WeatherKind.Ensoleille => "Ensoleillé",
        WeatherKind.Nuageux => "Nuageux",
        WeatherKind.Pluie => "Pluie",
        WeatherKind.Orage => "Orage",
        WeatherKind.Vent => "Vent fort",
        _ => w.ToString()
    };

    public static string Of(HealthLevel h) => h switch
    {
        HealthLevel.Good => "Sain",
        HealthLevel.Warning => "À surveiller",
        HealthLevel.Critical => "Critique",
        _ => h.ToString()
    };
}

/// <summary>Formatage des montants et dates (devise par défaut : XAF / FCFA).</summary>
public static class Money
{
    private static readonly CultureInfo Fr = CultureInfo.GetCultureInfo("fr-FR");

    public static string Format(decimal amount, string currency = "FCFA")
    {
        var number = amount.ToString("N0", Fr).Replace('\u202f', '\u00a0').Replace(' ', '\u00a0');
        return $"{number}\u00a0{currency}";
    }

    /// <summary>Format compact : 1,2 M / 850 k.</summary>
    public static string Compact(decimal amount, string currency = "FCFA")
    {
        var abs = Math.Abs(amount);
        string text;
        if (abs >= 1_000_000_000m) text = (amount / 1_000_000_000m).ToString("0.##", Fr) + "\u00a0Md";
        else if (abs >= 1_000_000m) text = (amount / 1_000_000m).ToString("0.##", Fr) + "\u00a0M";
        else if (abs >= 1_000m) text = (amount / 1_000m).ToString("0.#", Fr) + "\u00a0k";
        else text = amount.ToString("0", Fr);
        return $"{text}\u00a0{currency}";
    }

    public static string Percent(decimal ratio) => (ratio * 100m).ToString("0", Fr) + "\u00a0%";

    public static string Date(DateTime d) => d.ToString("dd MMM yyyy", Fr);
}
