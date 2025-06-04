using Azure.Storage.Queues.Models;
using BOTGC.API.Interfaces;

namespace BOTGC.API.Models
{
    public class QueueMessageWrapper<T> : IQueueMessage<T>
    {
        public QueueMessage Message { get; set; }
        public T? Payload { get; set; }

        public QueueMessageWrapper(QueueMessage message, T? payload)
        {
            Message = message;
            Payload = payload;
        }
    }

    public class DeadLetterEnvelope<T>
    {
        public T? OriginalMessage { get; set; } = default!;
        public long DequeueCount { get; set; }
        public DateTime FailedAt { get; set; }
    }

}
