using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Interfaces;

public interface ICheckRideReportRepository
{
    Task CreateAsync(CheckRideReportRecord record, CancellationToken cancellationToken = default);
    Task<CheckRideReportRecord?> GetSignedOffReportAsync(int memberId, CancellationToken cancellationToken = default);
}
