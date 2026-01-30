namespace BOTGC.MemberPortal.Models;

public sealed class MemberSearchResult
{
    public int? PlayerId { get; init; }

    public int? MemberNumber { get; init; }

    public string FullName { get; init; } = string.Empty;

    public string? MembershipCategory { get; init; }

    public DateTime? DateOfBirth { get; init; }
    public string? AvatarDataUri { get; set; }
}