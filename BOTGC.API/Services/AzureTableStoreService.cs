using System.Linq.Expressions;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using Azure;
using Azure.Data.Tables;
using System.Net.Sockets;
using BOTGC.API.Interfaces;
using Microsoft.Extensions.Logging;

namespace BOTGC.API.Services;

public sealed class AzureTableStoreService<T>(TableClient client, ILogger<AzureTableStoreService<T>> logger)
    : ITableStore<T> where T : class, ITableEntity, new()
{
    private readonly TableClient _client = client;
    private readonly ILogger _logger = logger;

    public async Task<T?> GetAsync(string partitionKey, string rowKey, CancellationToken ct)
    {
        var res = await AzureTransientRetry.ExecuteAsync(
            operation: c => _client.GetEntityIfExistsAsync<T>(partitionKey, rowKey, cancellationToken: c),
            onRetry: (attempt, delay, ex) =>
                _logger.LogWarning(ex, "Transient Table GET retry {Attempt} in {DelayMs}ms for {Pk}/{Rk}.", attempt, (int)delay.TotalMilliseconds, partitionKey, rowKey),
            ct: ct
        );

        if (!res.HasValue)
        {
            return null;
        }

        return res.Value;
    }

    public async Task<IReadOnlyList<T>> QueryAsync(Expression<Func<T, bool>> predicate, int take, CancellationToken cancellationToken = default)
    {
        if (predicate == null)
        {
            throw new ArgumentNullException(nameof(predicate));
        }

        if (take <= 0)
        {
            return Array.Empty<T>();
        }

        var results = new List<T>(capacity: Math.Min(take, 128));

        var filter = TableClient.CreateQueryFilter(predicate);

        await AzureTransientRetry.ExecuteAsync(
            operation: async ct =>
            {
                await foreach (var entity in _client.QueryAsync<T>(
                    filter: filter,
                    maxPerPage: Math.Min(take, 1000),
                    cancellationToken: ct).WithCancellation(ct))
                {
                    results.Add(entity);

                    if (results.Count >= take)
                    {
                        break;
                    }
                }
            },
            onRetry: (attempt, delay, ex) =>
                _logger.LogWarning(ex, "Transient Table QUERY retry {Attempt} in {DelayMs}ms (type {Type}, filter {Filter}).", attempt, (int)delay.TotalMilliseconds, typeof(T).Name, filter),
            ct: cancellationToken
        );

        return results;
    }


    public Task UpsertAsync(T entity, CancellationToken ct)
        => AzureTransientRetry.ExecuteAsync(
            operation: c => _client.UpsertEntityAsync(entity, TableUpdateMode.Replace, c),
            onRetry: (attempt, delay, ex) =>
                _logger.LogWarning(ex, "Transient Table UPSERT retry {Attempt} in {DelayMs}ms for {Pk}/{Rk}.", attempt, (int)delay.TotalMilliseconds, entity.PartitionKey, entity.RowKey),
            ct: ct
        );

    public async Task UpsertBatchAsync(IEnumerable<T> entities, CancellationToken ct)
    {
        foreach (var group in entities.GroupBy(e => e.PartitionKey))
        {
            var batch = new List<TableTransactionAction>(100);

            foreach (var e in group)
            {
                batch.Add(new TableTransactionAction(TableTransactionActionType.UpsertReplace, e));

                if (batch.Count == 100)
                {
                    await SubmitBatchAsync(batch, ct);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await SubmitBatchAsync(batch, ct);
            }
        }
    }

    public async Task DeleteAsync(string partitionKey, string rowKey, CancellationToken ct)
    {
        try
        {
            await AzureTransientRetry.ExecuteAsync(
                operation: c => _client.DeleteEntityAsync(partitionKey, rowKey, ETag.All, c),
                onRetry: (attempt, delay, ex) =>
                    _logger.LogWarning(ex, "Transient Table DELETE retry {Attempt} in {DelayMs}ms for {Pk}/{Rk}.", attempt, (int)delay.TotalMilliseconds, partitionKey, rowKey),
                ct: ct
            );
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
        }
    }

    public async IAsyncEnumerable<T> QueryByPartitionAsync(
        string partitionKey,
        string? odataFilter,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var filter = $"PartitionKey eq '{partitionKey.Replace("'", "''")}'";

        if (!string.IsNullOrWhiteSpace(odataFilter))
        {
            filter += $" and ({odataFilter})";
        }

        var attempt = 0;

        while (true)
        {
            attempt++;

            AsyncPageable<T> query;

            try
            {
                query = _client.QueryAsync<T>(filter: filter, cancellationToken: ct);
            }
            catch (Exception ex) when (attempt < 5 && IsTransient(ex))
            {
                var delay = TimeSpan.FromMilliseconds(Math.Min(10_000, 250 * Math.Pow(2, attempt - 1)) + Random.Shared.Next(0, 250));
                _logger.LogWarning(ex, "Transient Table QUERY (partition) retry {Attempt} in {DelayMs}ms for {Pk}.", attempt, (int)delay.TotalMilliseconds, partitionKey);
                await Task.Delay(delay, ct);
                continue;
            }

            await foreach (var ent in query.WithCancellation(ct))
            {
                yield return ent;
            }

            yield break;
        }
    }

    private Task SubmitBatchAsync(List<TableTransactionAction> batch, CancellationToken ct)
        => AzureTransientRetry.ExecuteAsync(
            operation: c => _client.SubmitTransactionAsync(batch, c),
            onRetry: (attempt, delay, ex) =>
                _logger.LogWarning(ex, "Transient Table TXN retry {Attempt} in {DelayMs}ms (PartitionKey {Pk}, Size {Size}).", attempt, (int)delay.TotalMilliseconds, GetPartitionKey(batch), batch.Count),
            ct: ct
        );

    private static string GetPartitionKey(List<TableTransactionAction> batch)
        => batch.Count == 0 ? string.Empty : ((ITableEntity)batch[0].Entity).PartitionKey;

    private static bool IsTransient(Exception ex)
    {
        if (ex is OperationCanceledException)
        {
            return false;
        }

        if (ex is RequestFailedException rfe)
        {
            if (rfe.Status == 408 || rfe.Status == 429) return true;
            if (rfe.Status >= 500 && rfe.Status <= 599) return true;
        }

        if (ex is HttpRequestException) return true;
        if (ex is IOException) return true;
        if (ex is SocketException) return true;

        if (ex.InnerException != null)
        {
            return IsTransient(ex.InnerException);
        }

        return false;
    }
}


internal static class AzureTransientRetry
{
    public static async Task<T> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Action<int, TimeSpan, Exception>? onRetry,
        CancellationToken ct)
    {
        const int maxAttempts = 5;

        Exception? last = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            try
            {
                return await operation(ct);
            }
            catch (Exception ex) when (IsTransient(ex))
            {
                last = ex;

                if (attempt == maxAttempts)
                {
                    throw;
                }

                var delay = ComputeDelay(attempt);
                onRetry?.Invoke(attempt, delay, ex);
                await Task.Delay(delay, ct);
            }
        }

        throw last ?? new InvalidOperationException("Retry failed with no captured exception.");
    }

    public static Task ExecuteAsync(
        Func<CancellationToken, Task> operation,
        Action<int, TimeSpan, Exception>? onRetry,
        CancellationToken ct)
        => ExecuteAsync<object?>(
            async c =>
            {
                await operation(c);
                return null;
            },
            onRetry,
            ct
        );

    private static TimeSpan ComputeDelay(int attempt)
    {
        var baseMs = Math.Min(10_000, (int)(250 * Math.Pow(2, attempt - 1)));
        var jitterMs = Random.Shared.Next(0, 250);
        return TimeSpan.FromMilliseconds(baseMs + jitterMs);
    }

    private static bool IsTransient(Exception ex)
    {
        if (ex is OperationCanceledException)
        {
            return false;
        }

        if (ex is RequestFailedException rfe)
        {
            if (rfe.Status == 408 || rfe.Status == 429) return true;
            if (rfe.Status >= 500 && rfe.Status <= 599) return true;
        }

        if (ex is HttpRequestException) return true;
        if (ex is IOException) return true;
        if (ex is SocketException) return true;

        if (ex.InnerException != null)
        {
            return IsTransient(ex.InnerException);
        }

        return false;
    }
}

