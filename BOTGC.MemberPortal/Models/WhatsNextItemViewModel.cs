namespace BOTGC.MemberPortal.Models;

public sealed class WhatsNextItemViewModel
{
    public required string Title { get; init; }
    public required string Subtitle { get; init; }
    public required string Href { get; init; }
    public required string IconUrl { get; init; }

    public DateTimeOffset? StartUtc { get; init; }
    public int Priority { get; init; }
    public string? Source { get; init; }
}
