using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Services;

public sealed class DashboardService : IDashboardService
{
    private readonly AppSettings _settings;
    private readonly IEnumerable<IWhatsNextProvider> _providers;

    public DashboardService(AppSettings settings, IEnumerable<IWhatsNextProvider> providers)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _providers = providers ?? throw new ArgumentNullException(nameof(providers));
    }

    public async Task<DashboardBuildResult> BuildDashboardAsync(MemberContext member, CancellationToken cancellationToken)
    {
        if (member is null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        var quickActions = new List<QuickActionViewModel>
        {
            new()
            {
                Title = "My Progress",
                Subtitle = "View your learning journey and unlock new milestones.",
                Href = "/progress",
                IconUrl = "/img/icons/nav_progress.png"
            },
            new()
            {
                Title = "Quizzers & Tasks",
                Subtitle = "Complete daily activities to boost your progress.",
                Href = "/quiz",
                IconUrl = "/img/icons/nav_play.png"
            },
            new()
            {
                Title = "Vouchers",
                Subtitle = "Earn rewards and redeem your vouchers anytime.",
                Href = "/vouchers",
                IconUrl = "/img/icons/nav_vouchers.png"
            },
            new()
            {
                Title = "Golf Buddy",
                Subtitle = "Chat with your buddy for support and quick guidance.",
                Href = "/mentor",
                IconUrl = "/img/icons/nav_buddy.png"
            }
        };

        var personalised = new List<WhatsNextItemViewModel>();

        foreach (var provider in _providers)
        {
            var items = await provider.GetItemsAsync(member, cancellationToken);
            if (items.Count > 0)
            {
                personalised.AddRange(items);
            }
        }

        var now = DateTimeOffset.UtcNow;

        var merged = personalised
            .Where(i => i.StartUtc == null || i.StartUtc >= now.AddDays(-1))
            .OrderBy(i => i.StartUtc ?? DateTimeOffset.MaxValue)
            .ThenByDescending(i => i.Priority)
            .ThenBy(i => i.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var generic = (_settings.WhatsNext?.GenericItems ?? Array.Empty<WhatsNextGenericItemSettings>())
            .Select(g => new WhatsNextItemViewModel
            {
                Title = g.Title,
                Subtitle = g.Subtitle,
                Href = g.Href,
                IconUrl = g.IconUrl,
                StartUtc = g.StartUtc,
                Priority = g.Priority,
                Source = "generic"
            })
            .Where(i => i.StartUtc == null || i.StartUtc >= now.AddDays(-1))
            .OrderBy(i => i.StartUtc ?? DateTimeOffset.MaxValue)
            .ThenByDescending(i => i.Priority)
            .ToList();

        foreach (var g in generic)
        {
            if (merged.Count >= (_settings.WhatsNext?.MaxItems ?? 2))
            {
                break;
            }

            var duplicate = merged.Any(m =>
                string.Equals(m.Title, g.Title, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(m.Href, g.Href, StringComparison.OrdinalIgnoreCase));

            if (!duplicate)
            {
                merged.Add(g);
            }
        }

        merged = merged
            .OrderBy(i => i.StartUtc ?? DateTimeOffset.MaxValue)
            .ThenByDescending(i => i.Priority)
            .Take(_settings.WhatsNext?.MaxItems ?? 2)
            .ToList();

        return new DashboardBuildResult
        {
            AvatarUrl = "/img/buddy.jpg",
            LevelLabel = "Cadet Level",
            LevelName = "Beginner",
            LevelProgressPercent = 34,
            QuickActions = quickActions,
            WhatsNext = merged
        };
    }
}
