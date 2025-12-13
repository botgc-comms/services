using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace BOTGC.MemberPortal.Controllers;

[Authorize]
public sealed class VouchersController : Controller
{
    private readonly ILogger<VouchersController> _logger;
    private readonly ICurrentUserService _currentUserService;
    private readonly IVoucherService _voucherService;

    public VouchersController(
        ILogger<VouchersController> logger,
        ICurrentUserService currentUserService,
        IVoucherService voucherService)
    {
        _logger = logger;
        _currentUserService = currentUserService;
        _voucherService = voucherService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var memberId = _currentUserService.UserId.Value;
        var vouchers = await _voucherService.GetVouchersForMemberAsync(memberId, cancellationToken);

        var summaries = vouchers
            .Where(v => v.IsActive)
            .GroupBy(v => v.TypeKey, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var first = g.First();
                return new VoucherTypeSummaryViewModel
                {
                    TypeKey = g.Key,
                    Title = first.TypeTitle,
                    Description = first.TypeDescription,
                    Image = first.Image,
                    AvailableCount = g.Count(v => v.IsActive),
                    BackgroundColor = null
                };
            })
            .OrderBy(x => x.Title)
            .ToList();

        return View(summaries);
    }

    [HttpGet]
    public async Task<IActionResult> Show(string typeKey, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(typeKey))
        {
            return RedirectToAction(nameof(Index));
        }

        if (!_currentUserService.UserId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var memberId = _currentUserService.UserId.Value;
        var vouchers = await _voucherService.GetVouchersForMemberAsync(memberId, cancellationToken);

        var voucher = vouchers
            .Where(v => v.IsActive)
            .FirstOrDefault(v => string.Equals(v.TypeKey, typeKey, StringComparison.OrdinalIgnoreCase));

        if (voucher == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var model = new VoucherDisplayViewModel
        {
            TypeKey = voucher.TypeKey,
            Title = voucher.TypeTitle,
            Description = voucher.TypeDescription,
            Code = voucher.Code
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Qr(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return NotFound();
        }

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(code, QRCodeGenerator.ECCLevel.Q);
        var png = new PngByteQRCode(data);
        var bytes = png.GetGraphic(20);

        return File(bytes, "image/png");
    }
}
