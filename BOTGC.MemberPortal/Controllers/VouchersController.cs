using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BOTGC.MemberPortal.Controllers;

[Authorize]
public sealed class VouchersController : Controller
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IVoucherService _voucherService;

    public VouchersController(
        ICurrentUserService currentUserService,
        IVoucherService voucherService)
    {
        _currentUserService = currentUserService;
        _voucherService = voucherService;
    }

    [HttpGet("/vouchers")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var vouchers = await _voucherService.GetVouchersForMemberAsync(_currentUserService.UserId.Value, cancellationToken);

        var model = new VouchersPageViewModel
        {
            Active = vouchers.Where(v => !v.IsUsed).OrderBy(v => v.TypeTitle).ToList(),
            Used = vouchers.Where(v => v.IsUsed).OrderByDescending(v => v.TypeTitle).ToList(),
            Expired = new List<VoucherViewModel>()
        };

        return View(model);
    }
}
