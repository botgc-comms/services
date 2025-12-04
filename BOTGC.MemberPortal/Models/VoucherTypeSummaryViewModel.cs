namespace BOTGC.MemberPortal.Models;

public sealed class VoucherTypeSummaryViewModel
{
    public string TypeKey { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public int AvailableCount { get; init; }
    public string? BackgroundColor { get; init; }
}