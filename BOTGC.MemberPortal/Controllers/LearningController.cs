// Controllers/LearningController.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BOTGC.MemberPortal.Common;
using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;
using BOTGC.MemberPortal.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BOTGC.MemberPortal.Controllers;

[Authorize]
[Route("learning")]
public sealed class LearningController : Controller
{
    private readonly ICurrentUserService _currentUserService;
    private readonly LearningPackService _packs;
    private readonly LearningMarkdownRenderer _markdown;
    private readonly AppSettings _appSettings; 
    
    public LearningController(
        ICurrentUserService currentUserService,
        LearningPackService packs,
        LearningMarkdownRenderer markdown,
        AppSettings settings)
    {
        _currentUserService = currentUserService;
        _packs = packs;
        _markdown = markdown;
        _appSettings = settings;
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var userId = _currentUserService.UserId.Value.ToString();
        var category = _currentUserService.Category;

        var mandatoryCategories = _currentUserService.IsParent
            ? _currentUserService.ChildLinks
                .Select(x => x.Category)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : (string.IsNullOrWhiteSpace(category) ? Array.Empty<string>() : new[] { category });

        var catalogue = await _packs.ListAvailableAsync(cancellationToken);
        var progress = await _packs.ListUserProgressAsync(userId, cancellationToken);

        var progressByPack = progress
            .GroupBy(x => x.PackId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var items = catalogue.Packs
            .Select(p =>
            {
                progressByPack.TryGetValue(p.Id, out var pr);

                var isVisited = pr?.FirstViewedAtUtc.HasValue == true;
                var isCompleted = pr?.IsCompleted == true;
                var mandatory = IsMandatoryForAnyCategory(p.MandatoryFor, mandatoryCategories);

                return new LearningPackListItemViewModel
                {
                    PackId = p.Id,
                    Title = p.Title,
                    Summary = p.Summary,
                    EstimatedMinutes = p.EstimatedMinutes,
                    IsMandatory = mandatory,
                    IsVisited = isVisited,
                    IsCompleted = isCompleted,
                    LastViewedAtUtc = pr?.LastViewedAtUtc,
                    CompletedAtUtc = pr?.CompletedAtUtc
                };
            })
            .OrderByDescending(x => x.IsMandatory)
            .ThenBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var vm = new LearningIndexViewModel
        {
            DisplayName = _currentUserService.DisplayName ?? "Junior member",
            Packs = items
        };

        return View("Index", vm);
    }

    [HttpGet("{packId}")]
    public async Task<IActionResult> Pack([FromRoute] string packId, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var userId = _currentUserService.UserId.Value.ToString();
        var category = _currentUserService.Category;

        var mandatoryCategories = _currentUserService.IsParent
            ? _currentUserService.ChildLinks
                .Select(x => x.Category)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray()
            : (string.IsNullOrWhiteSpace(category) ? Array.Empty<string>() : new[] { category });


        var pack = await _packs.GetPackAsync(packId, cancellationToken);
        if (pack is null)
        {
            return RedirectToAction(nameof(Index));
        }

        await _packs.RecordPackViewedAsync(userId, pack.Manifest.Id, cancellationToken);

        var progress = await _packs.GetUserProgressAsync(userId, pack.Manifest.Id, cancellationToken);

        var read = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (progress?.ReadPageIds is not null)
        {
            foreach (var id in progress.ReadPageIds)
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    read.Add(id);
                }
            }
        }

        var nextUnread = pack.Manifest.Pages.FirstOrDefault(p => !read.Contains(p.Id));
        var continueHref = nextUnread is not null
            ? $"/learning/{pack.Manifest.Id}/page/{nextUnread.Id}"
            : null;

        var hasStarted = read.Count > 0;

        var pages = pack.Manifest.Pages
            .Select((p, idx) => new LearningPackPageListItemViewModel
            {
                PageId = p.Id,
                Title = p.Title,
                Index = idx + 1,
                IsRead = read.Contains(p.Id),
                IsCurrent = hasStarted && nextUnread is not null && string.Equals(nextUnread.Id, p.Id, StringComparison.OrdinalIgnoreCase),
                Summary = p.Summary
            })
            .ToList();

        var vm = new LearningPackViewModel
        {
            PackId = pack.Manifest.Id,
            Title = pack.Manifest.Title,
            Summary = pack.Manifest.Summary,
            Description = pack.Manifest.Description,
            EstimatedMinutes = pack.Manifest.EstimatedMinutes,
            IsMandatory = IsMandatoryForAnyCategory(pack.Manifest.MandatoryFor, mandatoryCategories),
            IsCompleted = progress?.IsCompleted == true,
            CompletedAtUtc = progress?.CompletedAtUtc,
            ContinueHref = continueHref,
            Pages = pages
        };

        return View("Pack", vm);
    }

    [HttpGet("{packId}/page/{pageId}")]
    public async Task<IActionResult> Page([FromRoute] string packId, [FromRoute] string pageId, CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        var userId = _currentUserService.UserId.Value.ToString();

        var pack = await _packs.GetPackAsync(packId, cancellationToken);
        if (pack is null)
        {
            return RedirectToAction(nameof(Index));
        }

        var page = pack.Pages.FirstOrDefault(p => string.Equals(p.Id, pageId, StringComparison.OrdinalIgnoreCase));
        if (page is null)
        {
            return RedirectToAction(nameof(Pack), new { packId = pack.Manifest.Id });
        }

        await _packs.RecordPageViewedAsync(userId, pack.Manifest.Id, page.Id, cancellationToken);

        var manifestPageIndex = pack.Manifest.Pages
            .Select((p, idx) => (p, idx))
            .Where(x => string.Equals(x.p.Id, page.Id, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.idx)
            .DefaultIfEmpty(0)
            .First();

        var prev = manifestPageIndex > 0 ? pack.Manifest.Pages[manifestPageIndex - 1] : null;
        var next = manifestPageIndex < pack.Manifest.Pages.Count - 1 ? pack.Manifest.Pages[manifestPageIndex + 1] : null;

        var progress = await _packs.GetUserProgressAsync(userId, pack.Manifest.Id, cancellationToken);

        if (progress is not null && !progress.IsCompleted)
        {
            var read = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (progress.ReadPageIds is not null)
            {
                foreach (var id in progress.ReadPageIds)
                {
                    if (!string.IsNullOrWhiteSpace(id))
                    {
                        read.Add(id);
                    }
                }
            }

            if (read.Count >= pack.Manifest.Pages.Count && pack.Manifest.Pages.Count > 0)
            {
                await _packs.MarkPackCompletedAsync(userId, pack.Manifest.Id, cancellationToken);
                progress = await _packs.GetUserProgressAsync(userId, pack.Manifest.Id, cancellationToken);
            }
        }

        var html = _markdown.ToHtml(page.Markdown, pack.AssetBaseUrl);

        var vm = new LearningPackPageViewModel
        {
            PackId = pack.Manifest.Id,
            PackTitle = pack.Manifest.Title,
            PageId = page.Id,
            PageTitle = page.Title,
            PageNumber = manifestPageIndex + 1,
            TotalPages = pack.Manifest.Pages.Count,
            Html = html,
            PreviousPageId = prev?.Id,
            NextPageId = next?.Id,
            IsCompleted = progress?.IsCompleted == true
        };

        return View("Page", vm);
    }

    [HttpGet("assets/{packId}/{*fileName}")]
    [AllowAnonymous]
    public async Task<IActionResult> Get(string packId, string fileName, CancellationToken ct)
    {
        var pack = await _packs.GetPackAsync(packId, ct);
        if (pack is null)
        {
            return NotFound();
        }

        var normalised = ImageHelpers.NormaliseAssetKey(fileName);

        var asset = pack.Assets.FirstOrDefault(a => string.Equals(a.FileName, normalised, StringComparison.OrdinalIgnoreCase));
        if (asset is null)
        {
            return NotFound();
        }

        Response.Headers["Cache-Control"] = "public, max-age=86400";
        return File(asset.Bytes, asset.ContentType);
    }
    private static bool IsMandatoryForAnyCategory(IReadOnlyList<string>? mandatoryFor, IReadOnlyList<string> categories)
    {
        if (mandatoryFor is null || mandatoryFor.Count == 0)
        {
            return false;
        }

        if (categories is null || categories.Count == 0)
        {
            return false;
        }

        for (var i = 0; i < categories.Count; i++)
        {
            var c = categories[i];
            if (string.IsNullOrWhiteSpace(c))
            {
                continue;
            }

            if (mandatoryFor.Any(x => string.Equals(x, c, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }
        }

        return false;
    }



    private static bool IsMandatoryForCategory(IReadOnlyList<string>? mandatoryFor, string? category)
    {
        if (mandatoryFor is null || mandatoryFor.Count == 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            return false;
        }

        return mandatoryFor.Any(c => string.Equals(c, category, StringComparison.OrdinalIgnoreCase));
    }
}
