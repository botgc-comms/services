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
    private readonly IDashboardService _dashboardService;
    private readonly IMemberProgressService _memberProgressService;

    public HomeController(
        ICurrentUserService currentUserService,
        IDashboardService dashboardService,
        IMemberProgressService memberProgressService)
    {
        _currentUserService = currentUserService;
        _dashboardService = dashboardService;
        _memberProgressService = memberProgressService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken cancellationToken)
    {
        if (!_currentUserService.UserId.HasValue)
        {
            return RedirectToAction("Login", "Account");
        }

        if (_currentUserService.IsAdmin)
        {
            var adminModel = new AdminDashboardViewModel
            {
                DisplayName = _currentUserService.DisplayName ?? "Admin",
                AvatarUrl = "/img/buddy.jpg",
                AdminActions = new List<QuickActionViewModel>
                {
                    new()
                    {
                        Title = "Child QR Codes",
                        Subtitle = "Search for child and parent sign-up QR codes.",
                        Href = "/admin/qr-codes",
                        IconUrl = "/img/icons/nav_qr.png"
                    },
                    new()
                    {
                        Title = "Assessment",
                        Subtitle = "Record whether a child is signed off and recommended tee colour.",
                        Href = "Admin/CheckRideReport",
                        IconUrl = "/img/icons/nav_checkgame.png"
                    },
                    new()
                    {
                        Title = "Award Vouchers",
                        Subtitle = "Grant vouchers for achievements and rewards.",
                        Href = "/admin/award-vouchers",
                        IconUrl = "/img/icons/nav_vouchers.png"
                    },
                    new()
                    {
                        Title = "Send Messages",
                        Subtitle = "Message members and parents with updates and reminders.",
                        Href = "/admin/messages",
                        IconUrl = "/img/icons/nav_messages.png"
                    }
                }
            };

            return View("AdminIndex", adminModel);
        }

        if (_currentUserService.IsParent)
        {
            var children = BuildChildrenFromCurrentUser();

            var selectedChildId = TryGetSelectedChildIdFromQuery(children);
            var selected = selectedChildId.HasValue
                ? children.FirstOrDefault(c => c.MemberNumber == selectedChildId.Value)
                : (children.Count == 1 ? children[0] : null);

            var parentModel = new ParentDashboardViewModel
            {
                DisplayName = _currentUserService.DisplayName ?? "Parent",
                AvatarUrl = "/img/buddy.jpg",
                Children = children,
                SelectedChildMemberNumber = selected?.MemberNumber,
                SelectedChildName = selected?.DisplayName,
                ChildActions = BuildParentChildActions(selected),
                ParentActions = BuildParentActions()
            };

            return View("ParentIndex", parentModel);
        }

        var memberId = _currentUserService.UserId.Value;

        var memberContext = new MemberContext
        {
            MemberId = memberId,
            JuniorCategory = "Junior 12-18"
        };

        var dashboard = await _dashboardService.BuildDashboardAsync(memberContext, cancellationToken);

        var progress = await _memberProgressService.GetMemberProgressAsync(memberId, cancellationToken);

        var model = new DashboardViewModel
        {
            DisplayName = _currentUserService.DisplayName ?? "Junior member",
            AvatarUrl = dashboard.AvatarUrl,
            LevelLabel = "Category",
            LevelName = progress?.Category ?? "Unknown",
            LevelProgressPercent = progress?.ProgressPercentRounded ?? 0,
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

    private List<ParentChildViewModel> BuildChildrenFromCurrentUser()
    {
        var links = _currentUserService.ChildLinks ?? Array.Empty<AppAuthChildLink>();

        var children = links
            .Where(x => x != null)
            .Select(x =>
            {
                var id = x.MembershipId;
                if (id <= 0) return null;

                var name = x.Name;
                if (string.IsNullOrWhiteSpace(name))
                {
                    name = $"Junior {id}";
                }

                return new ParentChildViewModel
                {
                    MemberNumber = id,
                    DisplayName = name
                };
            })
            .Where(x => x != null)
            .Cast<ParentChildViewModel>()
            .GroupBy(x => x.MemberNumber)
            .Select(g => g.First())
            .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return children;
    }

    private int? TryGetSelectedChildIdFromQuery(IReadOnlyCollection<ParentChildViewModel> children)
    {
        if (children == null || children.Count == 0)
        {
            return null;
        }

        if (!Request.Query.TryGetValue("child", out var v))
        {
            return null;
        }

        if (!int.TryParse(v.ToString(), out var parsed))
        {
            return null;
        }

        if (children.Any(c => c.MemberNumber == parsed))
        {
            return parsed;
        }

        return null;
    }

    private static List<QuickActionViewModel> BuildParentChildActions(ParentChildViewModel? selected)
    {
        if (selected == null)
        {
            return new List<QuickActionViewModel>
            {
                new()
                {
                    Title = "Add a junior",
                    Subtitle = "Link a junior to this account to view progress and vouchers.",
                    Href = "/parent/link",
                    IconUrl = "/img/icons/nav_qr.png"
                }
            };
        }

        return new List<QuickActionViewModel>
        {
            new()
            {
                Title = "Progress",
                Subtitle = $"View {selected.DisplayName}'s achievements and level.",
                Href = $"/parent/child/{selected.MemberNumber}/progress",
                IconUrl = "/img/icons/nav_checkgame.png"
            },
            new()
            {
                Title = "Vouchers",
                Subtitle = $"View {selected.DisplayName}'s vouchers and rewards.",
                Href = $"/parent/child/{selected.MemberNumber}/vouchers",
                IconUrl = "/img/icons/nav_vouchers.png"
            }
        };
    }

    private static List<QuickActionViewModel> BuildParentActions()
    {
        return new List<QuickActionViewModel>
        {
            new()
            {
                Title = "Learning",
                Subtitle = "Micro learning packs to support juniors.",
                Href = "/learning",
                IconUrl = "/img/icons/nav_learning.png"
            }
        };
    }
}
