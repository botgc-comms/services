namespace BOTGC.MemberPortal.Models;

public sealed class TriggerDetectorRequest
{
    public string DetectorName { get; set; } = string.Empty;
    public int MemberId { get; set; }
    public string? CorrelationId { get; set; }
    public int? QuietPeriodSeconds { get; set; }
}