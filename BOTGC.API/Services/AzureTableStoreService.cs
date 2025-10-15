// File: API/Services/AzureTableStoreService.cs
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;
using BOTGC.API.Interfaces;

namespace BOTGC.API.Services;

public sealed class AzureTableStoreService<T>(TableClient client) : ITableStore<T> where T : class, ITableEntity, new()
{
    private readonly TableClient _client = client;

    public async Task<T?> GetAsync(string partitionKey, string rowKey, CancellationToken ct)
    {
        try
        {
            var res = await _client.GetEntityAsync<T>(partitionKey, rowKey, cancellationToken: ct);
            return res.Value;
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public Task UpsertAsync(T entity, CancellationToken ct)
        => _client.UpsertEntityAsync(entity, TableUpdateMode.Replace, ct);

    public async Task UpsertBatchAsync(IEnumerable<T> entities, CancellationToken ct)
    {
        var batch = new List<TableTransactionAction>(100);
        foreach (var e in entities)
        {
            batch.Add(new TableTransactionAction(TableTransactionActionType.UpsertReplace, e));
            if (batch.Count == 100)
            {
                await _client.SubmitTransactionAsync(batch, ct);
                batch.Clear();
            }
        }
        if (batch.Count > 0) await _client.SubmitTransactionAsync(batch, ct);
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
        public async IAsyncEnumerable<T> QueryByPartitionAsync(string partitionKey, string? odataFilter, [EnumeratorCancellation] CancellationToken ct)
    {
        var filter = $"PartitionKey eq '{partitionKey.Replace("'", "''")}'";
        if (!string.IsNullOrWhiteSpace(odataFilter)) filter += $" and ({odataFilter})";
        var query = _client.QueryAsync<T>(filter: filter, cancellationToken: ct);
        await foreach (var ent in query)
        {
            yield return ent;
        }
    }
}
