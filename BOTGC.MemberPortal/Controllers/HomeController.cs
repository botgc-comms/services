using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BOTGC.ManagementReports.Controllers;

[Authorize]
public class HomeController(
    ILogger<HomeController> logger,
    ICurrentUserService currentUserService,
    ITileService tileService) : Controller
{
    private readonly ILogger<HomeController> _logger = logger;
    private readonly ICurrentUserService _currentUserService = currentUserService;
    private readonly ITileService _tileService = tileService;

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

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel
        {
            RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
        });
    }
}
