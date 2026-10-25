using System.Security.Claims;
using ChantierPro.Core.Models;

namespace ChantierPro.Api.Infrastructure;

public interface ICurrentUserContext
{
    bool IsAuthenticated { get; }
    Guid? UserId { get; }
    string Name { get; }
    string Email { get; }
    UserRole? Role { get; }
}

public class CurrentUserContext : ICurrentUserContext
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Principal => _httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid? UserId
    {
        get
        {
            var val = Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                      ?? Principal?.FindFirst("sub")?.Value;
            return Guid.TryParse(val, out var id) ? id : null;
        }
    }

    public string Name => Principal?.FindFirst(ClaimTypes.Name)?.Value ?? "Système";

    public string Email => Principal?.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;

    public UserRole? Role
    {
        get
        {
            var roleStr = Principal?.FindFirst(ClaimTypes.Role)?.Value;
            return Enum.TryParse<UserRole>(roleStr, true, out var role) ? role : null;
        }
    }
}
