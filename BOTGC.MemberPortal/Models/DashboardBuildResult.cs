namespace BOTGC.MemberPortal.Models;

public sealed class DashboardBuildResult
{
    public string? AvatarUrl { get; init; }

    public required string LevelLabel { get; init; }
    public required string LevelName { get; init; }
    public required int LevelProgressPercent { get; init; }

    public required IReadOnlyList<QuickActionViewModel> QuickActions { get; init; }
    public required IReadOnlyList<WhatsNextItemViewModel> WhatsNext { get; init; }
}
