using BOTGC.API.Interfaces;
using BOTGC.API.Models;

public sealed class TableStorageQuizAttemptReadStore(
    ITableStore<QuizAttemptEntity> attemptsTable) : IQuizAttemptReadStore
{
    private readonly ITableStore<QuizAttemptEntity> _attemptsTable = attemptsTable ?? throw new ArgumentNullException(nameof(attemptsTable));

    public async Task<IReadOnlyList<QuizAttemptEntity>> ListCompletedAttemptsAsync(
        string userId,
        DateTimeOffset? sinceUtc,
        int take,
        CancellationToken cancellationToken)
    {
        var raw = await _attemptsTable.QueryAsync(
            predicate: e => e.PartitionKey == userId && e.Status == "Finished",
            take: 1000,
            cancellationToken: cancellationToken);

        IEnumerable<QuizAttemptEntity> filtered = raw;

        if (sinceUtc.HasValue)
        {
            var cut = sinceUtc.Value;
            filtered = filtered.Where(x => x.FinishedAtUtc.HasValue && x.FinishedAtUtc.Value > cut);
        }

        return filtered
            .Where(x => x.FinishedAtUtc.HasValue)
            .OrderByDescending(x => x.FinishedAtUtc!.Value)
            .Take(take <= 0 ? 50 : take)
            .ToArray();
    }
}
