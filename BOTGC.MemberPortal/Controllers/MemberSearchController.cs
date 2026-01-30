using System.IO;
using System.Text.Json;
using BOTGC.MemberPortal.Extensions;
using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BOTGC.MemberPortal.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/MemberSearch")]
public sealed class MemberSearchController : Controller
{
    private readonly JsonSerializerOptions _json;
    private readonly IJuniorMemberDirectoryService _directory;

    public MemberSearchController(JsonSerializerOptions json, IJuniorMemberDirectoryService directory)
    {
        _directory = directory ?? throw new ArgumentNullException(nameof(directory));
        _json = json ?? throw new ArgumentNullException(nameof(json));
    }

    [HttpGet("")]
    public IActionResult Index([FromQuery] string? returnUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && !Url.IsLocalUrl(returnUrl))
        {
            returnUrl = null;
        }

        ViewData["ReturnUrl"] = returnUrl;
        return View();
    }

    [HttpGet("Change")]
    public IActionResult Change([FromQuery] string? returnUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && !Url.IsLocalUrl(returnUrl))
        {
            returnUrl = null;
        }

        HttpContext.Session.ClearSelectedChild();

        ViewData["ReturnUrl"] = returnUrl;

        return View("Index");
    }

    [HttpGet("Search")]
    public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] int? maxResults, CancellationToken cancellationToken)
    {
        var query = (q ?? string.Empty).Trim();

        if (query.Length < 2)
        {
            return Ok(Array.Empty<MemberSearchResult>());
        }

        var results = await _directory.SearchAsync(query, cancellationToken);

        return Ok(results.Take(maxResults ?? 10));
    }

    [HttpPost("Select")]
    [ValidateAntiForgeryToken]
    public IActionResult Select(
        [FromForm] int playerId,
        [FromForm] int? memberNumber,
        [FromForm] string? fullName,
        [FromForm] string? avatarDataUri,
        [FromForm] string? returnUrl)
    {
        var selected = new MemberSearchResult
        {
            PlayerId = playerId,
            MemberNumber = memberNumber,
            FullName = fullName ?? string.Empty,
            AvatarDataUri = avatarDataUri
        };

        HttpContext.Session.SetSelectedChild(selected, _json);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpPost("Clear")]
    [ValidateAntiForgeryToken]
    public IActionResult Clear([FromForm] string? returnUrl)
    {
        HttpContext.Session.ClearSelectedChild();

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }
}
