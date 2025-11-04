using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using static BOTGC.API.Models.BottleCalibrationEntity;

namespace BOTGC.API.Services;

public sealed class CompetitionPayoutStore(
    ITableStore<CompetitionPayoutHeaderEntity> headerStore,
    ITableStore<CompetitionPayoutWinnerEntity> winnerStore
) : ICompetitionPayoutStore
{
    private readonly ITableStore<CompetitionPayoutHeaderEntity> _headers = headerStore ?? throw new ArgumentNullException(nameof(headerStore));
    private readonly ITableStore<CompetitionPayoutWinnerEntity> _winners = winnerStore ?? throw new ArgumentNullException(nameof(winnerStore));

    public async Task<bool> ExistsAsync(int competitionId, DateTime competitionDate, CancellationToken ct)
    {
        var partition = competitionDate.ToString("yyyy-MM");
        var rowKey = competitionId.ToString();
        var entity = await _headers.GetAsync(partition, rowKey, ct);
        return entity is not null;
    }

    public async Task PersistAsync(CompetitionPayoutResultDto result, CancellationToken ct)
    {
        var header = CompetitionPayoutHeaderEntity.FromResult(result);
        await _headers.UpsertAsync(header, ct);

        if (result.Winners?.Count > 0)
        {
            foreach (var w in result.Winners)
            {
                var entity = CompetitionPayoutWinnerEntity.FromWinner(w);
                await _winners.UpsertAsync(entity, ct);
            }
        }
    }
}
