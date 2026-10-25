using ChantierPro.Core.Models;

namespace ChantierPro.Api.Dtos;

public record LoginRequest(string Email, string Password);

public record RefreshTokenRequest(string RefreshToken);

public record AuthResponse(
    string AccessToken,
    DateTime ExpiresAt,
    string RefreshToken,
    AppUser User);

public record RegisterUserRequest(
    string Name,
    string Email,
    string Password,
    UserRole Role);

public record UpdateUserRequest(
    string Name,
    UserRole Role,
    bool IsActive);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);
