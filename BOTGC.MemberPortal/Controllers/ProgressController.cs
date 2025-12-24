using BOTGC.MemberPortal.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BOTGC.MemberPortal.Controllers;

[Authorize]
public sealed class ProgressController : Controller
{
    private readonly ICurrentUserService _currentUserService;

    public ProgressController(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        return View();
    }
}
