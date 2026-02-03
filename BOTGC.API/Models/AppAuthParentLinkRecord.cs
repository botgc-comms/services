namespace BOTGC.API.Models;

public sealed class AppAuthParentLinkRecord
{
    public string Code { get; set; } = string.Empty;
    public DateTimeOffset IssuedUtc { get; set; }
    public DateTimeOffset ExpiresUtc { get; set; }

    public int ChildMembershipNumber { get; set; }
    public int ChildMembershipId { get; set; }

    public string ChildFirstName { get; set; } = string.Empty;
    public string ChildSurname { get; set; } = string.Empty;
    public string ChildCategory { get; set; } = string.Empty;
}
