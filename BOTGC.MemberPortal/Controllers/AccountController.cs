using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace BOTGC.MemberPortal.Controllers;

public sealed class AccountController : Controller
{
    private readonly IUserAuthenticationService _authService;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public AccountController(IUserAuthenticationService authService)
    {
        _authService = authService;
    }

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        var model = new LoginViewModel
        {
            ReturnUrl = returnUrl
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var user = await _authService.ValidateCredentialsAsync(
            model.Username,
            model.Password,
            cancellationToken);

        if (user == null)
        {
            ModelState.AddModelError(string.Empty, "Invalid username or password.");
            return View(model);
        }

        var isAdmin =
            string.Equals(user.Username, "admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(user.Username, "1", StringComparison.OrdinalIgnoreCase);

        var hasNumericUsername = int.TryParse(
            user.Username,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out var memberNumber);

        var childLinks = (user.Children ?? Array.Empty<AppAuthChildLink>())
            .Where(x => x != null)
            .Take(3)
            .ToArray();

        var isParent = !isAdmin && childLinks.Length > 0;

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString(CultureInfo.InvariantCulture)),
            new Claim("memberId", user.Id.ToString(CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, user.Username ?? string.Empty),
            new Claim("firstName", user.FirstName ?? string.Empty),
            new Claim("surname", user.LastName ?? string.Empty),
            new Claim("category", user.Category ?? string.Empty)
        };

        if (hasNumericUsername)
        {
            claims.Add(new Claim("memberNumber", memberNumber.ToString(CultureInfo.InvariantCulture)));
        }

        claims.Add(new Claim("child_links", JsonSerializer.Serialize(childLinks, JsonOptions)));

        if (isParent)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Parent"));
            claims.Add(new Claim("role", "Parent"));
        }
        else
        {
            claims.Add(new Claim(ClaimTypes.Role, "Child"));
            claims.Add(new Claim("role", "Child"));
        }

        if (isAdmin)
        {
            claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            claims.Add(new Claim("role", "Admin"));
        }

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = model.RememberMe
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties);

        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Index", "Home");
    }
}
