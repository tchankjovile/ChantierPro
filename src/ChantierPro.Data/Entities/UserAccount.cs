using ChantierPro.Core.Models;

namespace ChantierPro.Data.Entities;

/// <summary>
/// Compte utilisateur persistant (identifiants + rôle). Le modèle métier <see cref="AppUser"/> du Core
/// n'embarque volontairement aucun secret : il est dérivé de ce compte via <see cref="ToAppUser"/>.
/// </summary>
public class UserAccount
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;

    /// <summary>Adresse e-mail normalisée (minuscules, sans espaces) : sert d'identifiant de connexion.</summary>
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Consultant;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public AppUser ToAppUser() => new() { Id = Id, Name = Name, Email = Email, Role = Role };
}
