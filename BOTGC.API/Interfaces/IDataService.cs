using BOTGC.API.Common;
using BOTGC.API.Dto;

namespace BOTGC.API.Interfaces
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
        Task<ClubChampionshipLeaderBoardDto> GetClubChampionshipsLeaderboardAsync(string competitionId);

        Task<NewMemberApplicationResultDto?> SubmitNewMemberApplicationAsync(NewMemberApplicationDto newMember);

        Task<List<SecurityLogEntryDto>> GetMobileOrders(DateTime? forDate = null);
        Task<bool> SetMemberProperty(MemberProperties property, int memberId, string value);
        Task<List<MembershipCategoryGroupDto>> GetMembershipCategories();
    }

}
