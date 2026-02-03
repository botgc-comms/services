using BOTGC.API.Interfaces;
using BOTGC.API.Models;

public sealed class TableStorageLearningPackProgressReadStore(
    ITableStore<LearningPackProgressEntity> progressTable)
    : ILearningPackProgressReadStore
{
    private readonly ITableStore<LearningPackProgressEntity> _progressTable = progressTable ?? throw new ArgumentNullException(nameof(progressTable));

    public async Task<IReadOnlyList<LearningPackProgressRow>> ListCompletedPacksAsync(
        string userId,
        DateTimeOffset? sinceUtc,
        int take,
        CancellationToken cancellationToken)
    {
        var raw = await _progressTable.QueryAsync(
            predicate: e => e.PartitionKey == userId && e.IsCompleted,
            take: 1000,
            cancellationToken: cancellationToken);

        IEnumerable<LearningPackProgressEntity> filtered = raw;

        filtered = filtered.Where(x => x.CompletedAtUtc.HasValue);

        if (sinceUtc.HasValue)
        {
            var cut = sinceUtc.Value;
            filtered = filtered.Where(x => x.CompletedAtUtc!.Value > cut);
        }

        var limited = filtered
            .OrderByDescending(x => x.CompletedAtUtc!.Value)
            .Take(take <= 0 ? 50 : Math.Min(take, 500));

        return limited
            .Select(x => new LearningPackProgressRow
            {
                PackId = x.PackId,
                CompletedAtUtc = x.CompletedAtUtc!.Value
            })
            .ToArray();
    }
}