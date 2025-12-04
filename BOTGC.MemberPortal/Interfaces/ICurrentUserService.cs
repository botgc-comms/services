namespace BOTGC.MemberPortal.Interfaces;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    int? UserId { get; }
    string? Username { get; }
    string? DisplayName { get; }
}
