namespace BOTGC.MemberPortal.Models;
public sealed class DashboardTileViewModel
{
    public string Type { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }

    public string? Controller { get; init; }
    public string? Action { get; init; }

    public string? ImageUrl { get; init; }

    public int? ProgressPercent { get; init; }
    public string? ProgressLabel { get; init; }

    public string? BackgroundColor { get; init; }
}
