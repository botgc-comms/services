using System.Globalization;
using System.Security.Claims;
using BOTGC.MemberPortal.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace BOTGC.MemberPortal.Controllers;

[Route("app")]
public sealed class AppAuthController : Controller
{
    private const string CacheKeyPrefixWebSso = "AppAuth:WebSSO-";

    private readonly ICacheService _cacheService;

    public AppAuthController(ICacheService cacheService)
    {
        _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
    }

    [HttpGet("sso")]
    public async Task<IActionResult> Sso([FromQuery] string code, [FromQuery] string? returnUrl = null, CancellationToken cancellationToken = default)
    {
        Response.Headers.CacheControl = "no-store";

        code = (code ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        var key = CacheKeyPrefixWebSso + code;

        var record = await _cacheService.GetAsync<AppAuthWebSsoRecord>(key);
        if (record == null || record.ExpiresUtc <= DateTimeOffset.UtcNow)
        {
            await _cacheService.RemoveAsync(key);
            return RedirectToAction("Login", "Account", new { returnUrl });
        }

        await _cacheService.RemoveAsync(key);

        var displayName = BuildDisplayName(record.FirstName, record.Surname);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, record.MembershipId.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, displayName),
            new Claim("memberId", record.MembershipId.ToString(CultureInfo.InvariantCulture)),
            new Claim("memberNumber", record.MembershipNumber.ToString(CultureInfo.InvariantCulture)),
            new Claim("firstName", record.FirstName ?? string.Empty),
            new Claim("surname", record.Surname ?? string.Empty),
            new Claim("category", record.Category ?? string.Empty),
            new Claim("display_name", displayName),
            new Claim("client_id", record.ClientId ?? string.Empty),
            new Claim("jwt_jti", record.JwtJti ?? string.Empty),
            new Claim("jwt_sub", record.JwtSub ?? string.Empty)
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = false,
            AllowRefresh = true
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    private static string BuildDisplayName(string? firstName, string? surname)
    {
        var f = (firstName ?? string.Empty).Trim();
        var s = (surname ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(f) && string.IsNullOrWhiteSpace(s))
        {
            return "Member";
        }

        if (string.IsNullOrWhiteSpace(f))
        {
            return s;
        }

        if (string.IsNullOrWhiteSpace(s))
        {
            return f;
        }

        return $"{f} {s}";
    }

    public sealed class AppAuthWebSsoRecord
    {
        public string Code { get; set; } = string.Empty;
        public DateTimeOffset IssuedUtc { get; set; }
        public DateTimeOffset ExpiresUtc { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public string JwtJti { get; set; } = string.Empty;
        public string JwtSub { get; set; } = string.Empty;
        public int MembershipNumber { get; set; }
        public int MembershipId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
    }
}
