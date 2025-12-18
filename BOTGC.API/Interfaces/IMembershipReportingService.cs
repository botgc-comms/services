using BOTGC.API.Dto;

namespace BOTGC.API.Interfaces;

public interface IMembershipReportingService
{
    public Task<MembershipReportDto> GetManagementReport(CancellationToken cancellationToken);
    public Task<MembershipReportDto> GetManagementReport(DateTime asAtDate, CancellationToken cancellationToken);
}
