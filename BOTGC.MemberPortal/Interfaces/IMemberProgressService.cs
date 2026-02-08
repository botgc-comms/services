using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Interfaces;

public interface IMemberProgressService
{
    Task<MemberProgress?> GetMemberProgressAsync(int memberId, CancellationToken cancellationToken);
}