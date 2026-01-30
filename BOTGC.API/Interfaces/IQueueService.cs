using Azure.Storage.Queues;

namespace BOTGC.API.Interfaces;

public interface IQueueService<T>
{
    Task EnqueueAsync(T item, CancellationToken cancellationToken = default);
    Task DeadLetterEnqueueAsync(T item, long dequeueCount, DateTime? errorAt, Exception? lastError, CancellationToken cancellationToken = default);
    Task<IQueueMessage<T>[]> ReceiveMessagesAsync(int maxMessages, TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default);
    Task DeleteMessageAsync(string messageId, string popReceipt, CancellationToken cancellationToken = default);
    Task EnqueueManyAsync(IEnumerable<T> items, CancellationToken cancellationToken = default);
}
