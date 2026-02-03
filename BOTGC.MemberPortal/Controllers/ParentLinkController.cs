using System.Globalization;
using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace BOTGC.MemberPortal.Controllers;

[Authorize]
public sealed class ParentLinkController : Controller
{
    private readonly IParentLinkService _parentLinkService;
    private readonly AppSettings _settings;

    public ParentLinkController(IParentLinkService parentLinkService, AppSettings settings)
    {
        _parentLinkService = parentLinkService;
        _settings = settings;
    }

    [HttpGet("/parent-link")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var childMembershipId = GetIntClaim("memberId");
        var childMembershipNumber = GetIntClaim("memberNumber");
        var childFirstName = GetClaim("firstName") ?? string.Empty;
        var childSurname = GetClaim("surname") ?? string.Empty;
        var childCategory = GetClaim("category") ?? string.Empty;

        var (code, expiresUtc) = await _parentLinkService.CreateParentLinkAsync(
            childMembershipId,
            childMembershipNumber,
            childFirstName,
            childSurname,
            childCategory,
            cancellationToken);

        var redeemUrl = BuildApiRedeemUrl(code);

        var vm = new ParentLinkViewModel
        {
            Code = code,
            ExpiresUtc = expiresUtc,
            RedeemUrl = redeemUrl
        };

        return View(vm);
    }

    [HttpGet("/parent-link/qr.png")]
    public IActionResult Qr([FromQuery] string code)
    {
        code = (code ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(code))
        {
            return NotFound();
        }

        var redeemUrl = BuildApiRedeemUrl(code);

        byte[] png;
        using (var generator = new QRCodeGenerator())
        using (var qrData = generator.CreateQrCode(redeemUrl, QRCodeGenerator.ECCLevel.Q))
        {
            var qr = new PngByteQRCode(qrData);
            png = qr.GetGraphic(pixelsPerModule: 8);
        }

        Response.Headers.CacheControl = "no-store";
        return File(png, "image/png");
    }

    private string BuildApiRedeemUrl(string code)
    {
        var baseUrl = (_settings.API.Url ?? string.Empty).Trim().TrimEnd('/');
        return $"{baseUrl}/api/auth/app/redeem?code={Uri.EscapeDataString(code)}";
    }

    private string? GetClaim(string type)
    {
        return User?.Claims?.FirstOrDefault(c => string.Equals(c.Type, type, StringComparison.Ordinal))?.Value;
    }

    private int GetIntClaim(string type)
    {
        var value = GetClaim(type);
        if (string.IsNullOrWhiteSpace(value) || !int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            throw new InvalidOperationException($"Missing claim '{type}'.");
        }

        return parsed;
    }
}

