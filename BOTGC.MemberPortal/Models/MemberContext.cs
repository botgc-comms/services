namespace BOTGC.MemberPortal.Models;

public sealed class MemberContext
{
    public int MemberId { get; init; }
    public string JuniorCategory { get; init; } = string.Empty;
}
