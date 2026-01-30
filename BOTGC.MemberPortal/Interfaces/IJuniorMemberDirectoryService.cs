using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Interfaces;

public interface IJuniorMemberDirectoryService
{
    Task<IReadOnlyList<MemberSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default);
}