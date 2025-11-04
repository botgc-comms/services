using BOTGC.API.Dto;

namespace BOTGC.API.Interfaces
{
    public interface ICompetitionPayoutStore
    {
        Task<bool> ExistsAsync(int competitionId, DateTime competitionDate, CancellationToken ct);
        Task PersistAsync(CompetitionPayoutResultDto result, CancellationToken ct);
    }
}