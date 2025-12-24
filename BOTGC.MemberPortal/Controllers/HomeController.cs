using System.Diagnostics;
using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BOTGC.MemberPortal.Controllers;

[Authorize]
public sealed class HomeController : Controller
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITileService _tileService;

    public HomeController(ICurrentUserService currentUserService, ITileService tileService)
    {
        _currentUserService = currentUserService;
        _tileService = tileService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var juniorCategory = "Junior 12-18";

        var memberContext = new MemberContext
        {
            MemberId = _currentUserService.UserId.Value,
            JuniorCategory = juniorCategory
        };

        var tiles = await _tileService.GetTilesForMemberAsync(memberContext, cancellationToken);

        var model = new DashboardViewModel
        {
            DisplayName = _currentUserService.DisplayName ?? "Junior member",
            JuniorCategory = juniorCategory,
            Tiles = tiles
        };

        return View(model);
    }

    [HttpGet]
    public IActionResult Privacy()
    {
        return View();
    }

    [HttpGet]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
