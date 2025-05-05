using Azure.Storage.Queues.Models;
using Azure.Storage.Queues;
using BOTGC.API.Dto;

namespace BOTGC.API.Interfaces
{
    public interface IQueueService<T>
    {
        Task EnqueueAsync(T item, CancellationToken cancellationToken = default);
        Task DeadLetterEnqueueAsync(T item, long dequeueCount, DateTime? errorAt, CancellationToken cancellationToken = default);
        Task<IQueueMessage<T>[]> ReceiveMessagesAsync(int maxMessages, TimeSpan? visibilityTimeout = null, CancellationToken cancellationToken = default);
        Task DeleteMessageAsync(string messageId, string popReceipt, CancellationToken cancellationToken = default);

    }

    public interface IQueueMessage<T>
    {
        QueueMessage Message { get; set; }
        T Payload { get; set; }
    }
}
