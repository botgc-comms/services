using System.Globalization;
using System.Security.Claims;
using BOTGC.MemberPortal.Interfaces;

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
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var id =
                user.FindFirstValue("memberId")
                ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(id))
            {
                return null;
            }

            if (int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return null;
        }
    }

    public int? MemberNumber
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var v = user.FindFirstValue("memberNumber");
            if (string.IsNullOrWhiteSpace(v))
            {
                return null;
            }

            if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                return parsed;
            }

            return null;
        }
    }

    public string? Username
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return user.FindFirstValue(ClaimTypes.Name);
        }
    }

    public string? DisplayName
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return user.FindFirstValue("display_name")
                ?? user.FindFirstValue(ClaimTypes.Name);
        }
    }

    public string? FirstName =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("firstName");

    public string? Surname =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("surname");

    public string? Category =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("category");
}
