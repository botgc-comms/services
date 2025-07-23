using BOTGC.Leaderboards.Models;
using BOTGC.Leaderboards.Models.EclecticScorecard;

namespace BOTGC.Leaderboards.Interfaces;

public interface ILeaderboardService
{
    Task<LeaderboardViewModel?> GetLeaderboardPlayersAsync(int competitionId);
    Task<List<CompetitionDetailsViewModel>> GetCompetitionsAsync();
    Task<ClubChampionshipLeaderboardViewModel?> GetClubChampionshipLeaderboardPlayersAsync(int competitionId);
}