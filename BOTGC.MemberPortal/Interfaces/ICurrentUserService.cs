using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Interfaces;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    bool IsParent { get; }
    int? UserId { get; }
    string? Username { get; }
    string? DisplayName { get; }
    string? Category { get; }
    
    IReadOnlyCollection<AppAuthChildLink> ChildLinks { get; }

}
