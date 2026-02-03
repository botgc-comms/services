namespace BOTGC.MemberPortal.Models;

public sealed class ParentLinkViewModel
{
    public string Code { get; set; } = string.Empty;
    public DateTimeOffset ExpiresUtc { get; set; }
    public string RedeemUrl { get; set; } = string.Empty;
}