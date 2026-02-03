using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsParent
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            if (user.IsInRole("Parent"))
            {
                return true;
            }

            var role =
                user.FindFirstValue(ClaimTypes.Role)
                ?? user.FindFirstValue("role")
                ?? user.FindFirstValue("roles");

            if (string.IsNullOrWhiteSpace(role))
            {
                return false;
            }

            return string.Equals(role, "Parent", StringComparison.OrdinalIgnoreCase)
                || role.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .Any(r => string.Equals(r, "Parent", StringComparison.OrdinalIgnoreCase));
        }
    }

    public IReadOnlyCollection<AppAuthChildLink> ChildLinks
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return Array.Empty<AppAuthChildLink>();
            }

            var json = user.FindFirstValue("child_links");
            if (string.IsNullOrWhiteSpace(json))
            {
                return Array.Empty<AppAuthChildLink>();
            }

            try
            {
                var retVal = JsonSerializer.Deserialize<List<AppAuthChildLink>>(json, JsonOptions);
                if (retVal == null) return  Array.Empty<AppAuthChildLink>();
                return retVal;
            }
            catch
            {
                return Array.Empty<AppAuthChildLink>();
            }
        }
    }

    public bool IsAdmin
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return false;
            }

            if (user.IsInRole("Admin"))
            {
                return true;
            }

            var role =
                user.FindFirstValue(ClaimTypes.Role)
                ?? user.FindFirstValue("role")
                ?? user.FindFirstValue("roles");

            if (string.IsNullOrWhiteSpace(role))
            {
                return false;
            }

            return string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
                || role.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                       .Any(r => string.Equals(r, "Admin", StringComparison.OrdinalIgnoreCase));
        }
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

    public string? DisplayName =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("firstName");

    public string? FirstName =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("firstName");

    public string? Surname =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("surname");

    public string? Category =>
        _httpContextAccessor.HttpContext?.User?.FindFirstValue("category");
}
