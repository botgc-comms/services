using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Azure.Data.Tables;

namespace BOTGC.API.Interfaces;

public interface ITableStore<T> where T : class, ITableEntity, new()
{
    Task<T?> GetAsync(string partitionKey, string rowKey, CancellationToken ct);

    Task<IReadOnlyList<T>> QueryAsync(Expression<Func<T, bool>> predicate, int take, CancellationToken cancellationToken = default);

    IAsyncEnumerable<T> QueryByPartitionAsync(string partitionKey, string? odataFilter, CancellationToken ct);

    Task UpsertAsync(T entity, CancellationToken ct);

    Task UpsertBatchAsync(IEnumerable<T> entities, CancellationToken ct);

    Task DeleteAsync(string partitionKey, string rowKey, CancellationToken ct);
}
