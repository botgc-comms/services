using BOTGC.API.Dto;
using BOTGC.API.Models;
using static BOTGC.API.Models.BottleCalibrationEntity;

namespace BOTGC.API.Interfaces
{
    public interface ICompetitionPayoutStore
    {
        Task<bool> ExistsAsync(int competitionId, DateTime competitionDate, CancellationToken ct);
        Task PersistAsync(CompetitionPayoutResultDto result, CancellationToken ct);
        Task<IReadOnlyList<CompetitionPayoutHeaderEntity>> GetHeadersForYearAsync(int year, CancellationToken ct);
        IAsyncEnumerable<CompetitionPayoutWinnerEntity> GetWinnersByCompetitionAsync(int competitionId, CancellationToken ct);
    }
}