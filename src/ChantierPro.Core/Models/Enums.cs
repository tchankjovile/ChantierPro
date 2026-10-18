namespace ChantierPro.Core.Models;

/// <summary>Rôles utilisateurs (cahier des charges §3.1).</summary>
public enum UserRole
{
    Administrateur,
    ChefChantier,
    Magasinier,
    Consultant
}

/// <summary>Statuts d'un chantier (F-CHA-02).</summary>
public enum ChantierStatus
{
    Planifie,
    EnCours,
    Suspendu,
    Termine,
    Cloture
}

public enum AlertLevel
{
    Info,
    Avertissement,
    Critical
}

public enum AlertType
{
    Budget,
    Stock,
    Retard,
    Incoherence,
    Pointage
}

public enum MovementType
{
    Entree,
    Sortie
}

public enum AttendanceStatus
{
    Present,
    Absent,
    Retard,
    DemiJournee
}

public enum BudgetCategory
{
    Materiaux,
    MainOeuvre,
    LocationEngins,
    Transport,
    SousTraitance,
    Outillage,
    Administratif,
    Imprevus
}

public enum PaymentMode
{
    Especes,
    MobileMoney,
    Virement,
    Cheque,
    Carte
}

public enum WorkerStatus
{
    Actif,
    Inactif
}

public enum WeatherKind
{
    Ensoleille,
    Nuageux,
    Pluie,
    Orage,
    Vent
}

/// <summary>Niveau de santé global d'un chantier (vert / orange / rouge).</summary>
public enum HealthLevel
{
    Good,
    Warning,
    Critical
}
