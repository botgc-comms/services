using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using static BOTGC.API.Models.BottleCalibrationEntity;

namespace BOTGC.API.Services;

public sealed class CompetitionPayoutService(
        ITableStore<CompetitionPayoutHeaderEntity> headerStore,
        ITableStore<CompetitionPayoutWinnerEntity> winnerStore
    ) : ICompetitionPayoutStore
{
    private readonly ITableStore<CompetitionPayoutHeaderEntity> _headers = headerStore;
    private readonly ITableStore<CompetitionPayoutWinnerEntity> _winners = winnerStore;

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
        if (header.CompetitionDate.Kind != DateTimeKind.Utc) header.CompetitionDate = DateTime.SpecifyKind(header.CompetitionDate, DateTimeKind.Utc);
        await _headers.UpsertAsync(header, ct);

        if (result.Winners is not null && result.Winners.Count > 0)
        {
            foreach (var w in result.Winners)
            {
                var entity = CompetitionPayoutWinnerEntity.FromWinner(w);
                await _winners.UpsertAsync(entity, ct);
            }
        }
    }

    public async Task<IReadOnlyList<CompetitionPayoutHeaderEntity>> GetHeadersForYearAsync(int year, CancellationToken ct)
    {
        var list = new List<CompetitionPayoutHeaderEntity>();
        for (var m = 1; m <= 12; m++)
        {
            var pk = $"{year}-{m:00}";
            await foreach (var h in _headers.QueryByPartitionAsync(pk, null, ct))
            {
                list.Add(h);
            }
        }
        return list;
    }

    public async IAsyncEnumerable<CompetitionPayoutWinnerEntity> GetWinnersByCompetitionAsync(int competitionId, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
    {
        var pk = competitionId.ToString();
        await foreach (var w in _winners.QueryByPartitionAsync(pk, null, ct))
        {
            yield return w;
        }
    }
}