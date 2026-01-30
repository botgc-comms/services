using System.Globalization;
using System.Text.Json;
using BOTGC.MemberPortal.Extensions;
using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BOTGC.MemberPortal.Controllers;

[Authorize(Roles = "Admin")]
[Route("Admin/CheckRideReport")]
public sealed class CheckRideReportController : Controller
{
    private readonly ICurrentUserService _currentUser;
    private readonly ICheckRideReportRepository _repo;
    private readonly JsonSerializerOptions _json;

    public CheckRideReportController(
        ICurrentUserService currentUser,
        ICheckRideReportRepository repo,
        JsonSerializerOptions json)
    {
        _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
        _json = json ?? throw new ArgumentNullException(nameof(json));
    }

    [HttpGet("")]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        var selected = HttpContext.Session.GetSelectedChild(_json);

        if (selected is null || !selected.PlayerId.HasValue)
        {
            return RedirectToAction("Index", "MemberSearch", new { returnUrl = "/Admin/CheckRideReport" });
        }

        var signedOff = await _repo.GetSignedOffReportAsync(selected.MemberNumber!.Value, cancellationToken);

        var vm = new CheckRideReportViewModel()
        {
            PlayedOn = DateOnly.FromDateTime(DateTime.Now),
            IsAlreadySignedOff = signedOff is not null,
            SignedOffSummary = signedOff is null
                ? null
                : new CheckRideSignedOffSummaryViewModel
                {
                    PlayedOn = DateOnly.FromDateTime(signedOff.PlayedOn),
                    RecommendedTeeBox = signedOff.RecommendedTeeBox ?? string.Empty,
                    RecordedBy = signedOff.RecordedBy,
                    RecordedAtUtc = signedOff.RecordedAtUtc,
                },
        };

        return View(vm);
    }

    [HttpPost("")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(CheckRideReportViewModel model, CancellationToken cancellationToken)
    {
        var selected = HttpContext.Session.GetSelectedChild(_json);

        if (selected is null || !selected.PlayerId.HasValue)
        {
            return RedirectToAction("Index", "MemberSearch", new { returnUrl = "/Admin/CheckRideReport" });
        }

        var signedOff = await _repo.GetSignedOffReportAsync(selected.MemberNumber!.Value, cancellationToken);
        if (signedOff is not null)
        {
            ModelState.AddModelError(string.Empty, "This junior has already been signed off. No further check ride reports can be recorded.");

            model.IsAlreadySignedOff = true;
            model.SignedOffSummary = new CheckRideSignedOffSummaryViewModel
            {
                PlayedOn = DateOnly.FromDateTime(signedOff.PlayedOn),
                RecommendedTeeBox = signedOff.RecommendedTeeBox ?? string.Empty,
                RecordedBy = signedOff.RecordedBy,
                RecordedAtUtc = signedOff.RecordedAtUtc,
            };

            return View(model);
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var recordedBy =
            _currentUser.DisplayName
            ?? _currentUser.Username
            ?? _currentUser.UserId?.ToString(CultureInfo.InvariantCulture)
            ?? "admin";

        var record = new CheckRideReportRecord
        {
            MemberId = selected.MemberNumber!.Value,
            RecordedBy = recordedBy,
            RecordedAtUtc = DateTimeOffset.UtcNow,
            PlayedOn = DateOnlyAsUtc(model.PlayedOn!.Value),
            EtiquetteAndSafetyOk = model.EtiquetteAndSafetyOk!.Value,
            CareForCourseOk = model.CareForCourseOk!.Value,
            PaceAndImpactOk = model.PaceAndImpactOk!.Value,
            ReadyToPlayIndependently = model.ReadyToPlayIndependently!.Value,
            RecommendedTeeBox = model.RecommendedTeeBox!,
            Notes = string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes.Trim(),
        };

        await _repo.CreateAsync(record, cancellationToken);

        return RedirectToAction("Index", "Home");
    }

    private static DateTime DateOnlyAsUtc(DateOnly d)
    {
        return DateTime.SpecifyKind(d.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
    }
}
