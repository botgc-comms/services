using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Interfaces;

public interface ITileService
{
    Task<IReadOnlyList<DashboardTileViewModel>> GetTilesForMemberAsync(
        MemberContext member,
        CancellationToken cancellationToken = default);
}
