namespace ChantierPro.Data.Entities;

/// <summary>Jeton de rafraîchissement : seule l'empreinte SHA-256 est stockée, jamais le jeton en clair.</summary>
public class RefreshToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
