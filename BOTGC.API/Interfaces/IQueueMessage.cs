using Azure.Storage.Queues.Models;

namespace BOTGC.API.Interfaces
{
    public interface IQueueMessage<T>
    {
        QueueMessage Message { get; set; }
        T Payload { get; set; }
    }
}
