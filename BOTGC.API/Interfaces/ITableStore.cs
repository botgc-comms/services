using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Data.Tables;

namespace BOTGC.API.Interfaces
{
    public interface ITableStore<T> where T : class, ITableEntity, new()
    {
        Task<T?> GetAsync(string partitionKey, string rowKey, CancellationToken ct);
        Task UpsertAsync(T entity, CancellationToken ct);
        Task UpsertBatchAsync(IEnumerable<T> entities, CancellationToken ct);
        Task DeleteAsync(string partitionKey, string rowKey, CancellationToken ct);
        IAsyncEnumerable<T> QueryByPartitionAsync(string partitionKey, string? odataFilter, CancellationToken ct);
    }
}