using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Azure;
using Azure.Data.Tables;
using BOTGC.API.Interfaces;

namespace BOTGC.API.Services;

public sealed class AzureTableStoreService<T>(TableClient client) : ITableStore<T> where T : class, ITableEntity, new()
{
    private readonly TableClient _client = client;

    public async Task<T?> GetAsync(string partitionKey, string rowKey, CancellationToken ct)
    {
        var res = await _client.GetEntityIfExistsAsync<T>(partitionKey, rowKey, cancellationToken: ct);

        if (!res.HasValue)
        {
            return null;
        }

        return res.Value;
    }

    public async Task<IReadOnlyList<T>> QueryAsync(Expression<Func<T, bool>> predicate, int take, CancellationToken cancellationToken = default)
    {
        if (take <= 0)
        {
            return Array.Empty<T>();
        }

        var results = new List<T>(capacity: Math.Min(take, 128));

        await foreach (var entity in _client.QueryAsync(predicate, maxPerPage: take, cancellationToken: cancellationToken))
        {
            results.Add(entity);

            if (results.Count >= take)
            {
                break;
            }
        }

        return results;
    }

    public Task UpsertAsync(T entity, CancellationToken ct)
        => _client.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);

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
                    await _client.SubmitTransactionAsync(batch, ct);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
            {
                await _client.SubmitTransactionAsync(batch, ct);
            }
        }
    }

    public async Task DeleteAsync(string partitionKey, string rowKey, CancellationToken ct)
    {
        try
        {
            await _client.DeleteEntityAsync(partitionKey, rowKey, ETag.All, ct);
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

        var query = _client.QueryAsync<T>(filter: filter, cancellationToken: ct);

        await foreach (var ent in query.WithCancellation(ct))
        {
            yield return ent;
        }
    }
}
