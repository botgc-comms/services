using BOTGC.API.Dto;

namespace BOTGC.API.Interfaces
{
    public interface IMembershipReportingService
    {
        public Task<MembershipReportDto> GetManagementReport(CancellationToken cancellationToken);
    }
}