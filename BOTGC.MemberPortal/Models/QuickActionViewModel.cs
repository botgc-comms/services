namespace BOTGC.MemberPortal.Models;

public sealed class QuickActionViewModel
{
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required string Href { get; init; }
    public required string IconUrl { get; init; }
}
