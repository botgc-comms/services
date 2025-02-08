using Services.Dto;

namespace Services.Interfaces
{
    public interface IReportService
    {
        Task<List<MemberDto>> GetJuniorMembersAsync();
        Task<List<RoundDto>> GetRoundsByMemberIdAsync(string memberId);
        Task<ScorecardDto?> GetScorecardForRoundAsync(string roundId);
    }

}
