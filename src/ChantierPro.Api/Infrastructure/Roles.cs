namespace ChantierPro.Api.Infrastructure;

/// <summary>
/// Noms de rôles utilisés dans les attributs [Authorize]. Ils doivent rester identiques aux noms
/// des valeurs de <c>ChantierPro.Core.Models.UserRole</c> (vérifié par un test).
/// </summary>
public static class Roles
{
    public const string Admin = "Administrateur";
    public const string Chef = "ChefChantier";
    public const string Store = "Magasinier";
    public const string Viewer = "Consultant";

    /// <summary>Administrateur ou chef de chantier : gestion complète d'un chantier.</summary>
    public const string Managers = Admin + "," + Chef;

    /// <summary>Tous les rôles autorisés à écrire (hors consultation seule).</summary>
    public const string Contributors = Admin + "," + Chef + "," + Store;
}
