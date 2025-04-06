using BOTGC.API.Dto;
using Services.Dto;
using Services.Dtos;

namespace Services.Interfaces
{
    public interface IDataService
    {
        Task<List<MemberDto>> GetJuniorMembersAsync();
        Task<List<MemberDto>> GetCurrentMembersAsync();
        Task<List<RoundDto>> GetRoundsByMemberIdAsync(string memberId);
        Task<ScorecardDto?> GetScorecardForRoundAsync(string roundId);
        Task<TeeSheetDto?> GetTeeSheetByDateAsync(DateTime date);

        Task<List<MemberDto>> GetMembershipReportAsync();
        Task<List<MemberEventDto>> GetMembershipEvents(DateTime fromDate, DateTime toDate);

        Task<List<CompetitionDto>> GetActiveAndFutureCompetitionsAsync();
        Task<CompetitionSettingsDto> GetCompetitionSettingsAsync(string competitionId);
        Task<LeaderBoardDto> GetCompetitionLeaderboardAsync(string competitionId);

        Task<NewMemberApplicationResultDto?> SubmitNewMemberApplicationAsync(NewMemberApplicationDto newMember);
    }

}
