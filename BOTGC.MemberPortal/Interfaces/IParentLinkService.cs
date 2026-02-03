namespace BOTGC.MemberPortal.Interfaces;

public interface IParentLinkService
{
    Task<(string Code, DateTimeOffset ExpiresUtc)> CreateParentLinkAsync(
        int childMembershipId,
        int childMembershipNumber,
        string childFirstName,
        string childSurname,
        string childCategory,
        CancellationToken cancellationToken);
}