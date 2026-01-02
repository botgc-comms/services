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
    private readonly IDashboardService _whatsNextService;

    public HomeController(
        ICurrentUserService currentUserService,
        IDashboardService whatsNextService)
    {
        _currentUserService = currentUserService;
        _whatsNextService = whatsNextService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var memberContext = new MemberContext
        {
            MemberId = _currentUserService.UserId.Value,
            JuniorCategory = "Junior 12-18"
        };

        var dashboard = await _whatsNextService.BuildDashboardAsync(memberContext, cancellationToken);

        var model = new DashboardViewModel
        {
            DisplayName = _currentUserService.DisplayName ?? "Junior member",
            AvatarUrl = dashboard.AvatarUrl,
            LevelLabel = dashboard.LevelLabel,
            LevelName = dashboard.LevelName,
            LevelProgressPercent = dashboard.LevelProgressPercent,
            QuickActions = dashboard.QuickActions,
            WhatsNext = dashboard.WhatsNext
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
