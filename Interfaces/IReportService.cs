using Services.Dto;

namespace Services.Interfaces
{
    public interface IReportService
    {
        Task<List<MemberDto>> GetJuniorMembersAsync();
    }

}
