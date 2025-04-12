using BOTGC.Leaderboards.Models.EclecticScorecard;

namespace BOTGC.Leaderboards.Interfaces;

public interface IJuniorEclecticService
{
    Task<List<EclecticPlayerViewModel>> GetPlayersAsync(int? year = null);
}