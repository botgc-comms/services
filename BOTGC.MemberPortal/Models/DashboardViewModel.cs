namespace BOTGC.MemberPortal.Models;

public sealed class DashboardViewModel
{
    public required string DisplayName { get; init; }
    public string? AvatarUrl { get; init; }

    public required string LevelLabel { get; init; }
    public required string LevelName { get; init; }
    public int LevelProgressPercent { get; init; }

    public required IReadOnlyList<QuickActionViewModel> QuickActions { get; init; }
    public required IReadOnlyList<WhatsNextItemViewModel> WhatsNext { get; init; }
}
