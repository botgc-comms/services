using Services.Dto;

namespace Services.Interfaces
{
    public interface IDataService
    {
        Task<List<MemberDto>> GetJuniorMembersAsync();
        Task<List<RoundDto>> GetRoundsByMemberIdAsync(string memberId);
        Task<ScorecardDto?> GetScorecardForRoundAsync(string roundId);
    }

}
