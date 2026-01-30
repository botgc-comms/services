namespace BOTGC.MemberPortal.Models;

public sealed class CheckRideSignedOffSummaryViewModel
{
    public DateOnly PlayedOn { get; set; }

    public string RecommendedTeeBox { get; set; } = string.Empty;

    public string RecordedBy { get; set; } = string.Empty;

    public DateTimeOffset RecordedAtUtc { get; set; }
}
