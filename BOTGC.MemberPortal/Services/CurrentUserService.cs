using BOTGC.MemberPortal.Interfaces;
using System.Security.Claims;

namespace BOTGC.MemberPortal.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public int? UserId
    {
        get
        {
            var id = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(id, out var parsed))
            {
                return parsed;
            }

            return null;
        }
    }

    public string? Username =>
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Name)?.Value;

    public string? DisplayName =>
        _httpContextAccessor.HttpContext?.User?.FindFirst("display_name")?.Value;
}
