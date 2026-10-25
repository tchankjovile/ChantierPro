using ChantierPro.Api.Dtos;
using ChantierPro.Api.Infrastructure;
using ChantierPro.Data;
using ChantierPro.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ChantierPro.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokenService;
    private readonly IClock _clock;
    private readonly ICurrentUserContext _userContext;

    public AuthController(
        AppDbContext db,
        IPasswordHasher hasher,
        ITokenService tokenService,
        IClock clock,
        ICurrentUserContext userContext)
    {
        _db = db;
        _hasher = hasher;
        _tokenService = tokenService;
        _clock = clock;
        _userContext = userContext;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            throw Errors.Validation("L'email et le mot de passe sont obligatoires.");

        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);

        if (user == null || !_hasher.VerifyPassword(request.Password, user.PasswordHash))
            throw Errors.Unauthorized("Identifiants incorrects.");

        if (!user.IsActive)
            throw Errors.Forbidden("Ce compte utilisateur est désactivé.");

        var (accessToken, accessExpiry) = _tokenService.CreateAccessToken(user);
        var (refreshToken, refreshHash, refreshExpiry) = _tokenService.CreateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = refreshHash,
            CreatedAt = _clock.UtcNow,
            ExpiresAt = refreshExpiry
        });

        await _db.SaveChangesAsync();

        return Ok(new AuthResponse(accessToken, accessExpiry, refreshToken, user.ToAppUser()));
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
            throw Errors.Validation("Le jeton de rafraîchissement est requis.");

        var hash = _tokenService.HashToken(request.RefreshToken);
        var stored = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash);

        if (stored == null || stored.RevokedAt != null || stored.ExpiresAt <= _clock.UtcNow)
            throw Errors.Unauthorized("Jeton de rafraîchissement invalide ou expiré.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == stored.UserId);
        if (user == null || !user.IsActive)
            throw Errors.Forbidden("Utilisateur introuvable ou désactivé.");

        // Révoquer l'ancien token (rotation des refresh tokens)
        stored.RevokedAt = _clock.UtcNow;

        var (newAccess, accessExpiry) = _tokenService.CreateAccessToken(user);
        var (newRefresh, newHash, refreshExpiry) = _tokenService.CreateRefreshToken();

        _db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = newHash,
            CreatedAt = _clock.UtcNow,
            ExpiresAt = refreshExpiry
        });

        await _db.SaveChangesAsync();

        return Ok(new AuthResponse(newAccess, accessExpiry, newRefresh, user.ToAppUser()));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest? request)
    {
        if (!string.IsNullOrWhiteSpace(request?.RefreshToken))
        {
            var hash = _tokenService.HashToken(request.RefreshToken);
            var stored = await _db.RefreshTokens.FirstOrDefaultAsync(r => r.TokenHash == hash);
            if (stored != null)
            {
                stored.RevokedAt = _clock.UtcNow;
                await _db.SaveChangesAsync();
            }
        }
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<object>> Me()
    {
        if (!_userContext.UserId.HasValue) throw Errors.Unauthorized("Non authentifié.");
        var user = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == _userContext.UserId.Value)
                   ?? throw Errors.NotFound("Utilisateur");

        return Ok(new
        {
            user.Id,
            user.Name,
            user.Email,
            Role = user.Role.ToString(),
            user.IsActive,
            user.CreatedAt
        });
    }

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 6)
            throw Errors.Validation("Le nouveau mot de passe doit comporter au moins 6 caractères.");

        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == _userContext.UserId!.Value)
                   ?? throw Errors.NotFound("Utilisateur");

        if (!_hasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
            throw Errors.Validation("Mot de passe actuel incorrect.");

        user.PasswordHash = _hasher.HashPassword(request.NewPassword);

        // Révoquer tous les refresh tokens de l'utilisateur
        var tokens = await _db.RefreshTokens.Where(r => r.UserId == user.Id && r.RevokedAt == null).ToListAsync();
        foreach (var t in tokens) t.RevokedAt = _clock.UtcNow;

        await _db.SaveChangesAsync();
        return NoContent();
    }
}
