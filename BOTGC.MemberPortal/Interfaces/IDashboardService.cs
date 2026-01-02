using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Interfaces;

public interface IDashboardService
{
    Task<DashboardBuildResult> BuildDashboardAsync(MemberContext member, CancellationToken cancellationToken);
}
