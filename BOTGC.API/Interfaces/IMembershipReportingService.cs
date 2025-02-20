using Services.Dto;

namespace BOTGC.API.Interfaces
{
    public interface IMembershipReportingService
    {
        public Task<MembershipReportDto> GetManagementReport();
    }
}