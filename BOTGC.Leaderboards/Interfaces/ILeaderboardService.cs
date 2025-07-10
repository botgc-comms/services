using BOTGC.Leaderboards.Models;
using BOTGC.Leaderboards.Models.EclecticScorecard;

namespace BOTGC.Leaderboards.Interfaces;

public interface ILeaderboardService
{
    Task<LeaderboardViewModel?> GetPlayersAsync(int competitionId);
    Task<List<CompetitionDetailsViewModel>> GetCompetitionsAsync();
}