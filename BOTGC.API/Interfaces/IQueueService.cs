using BOTGC.API.Dto;

namespace BOTGC.API.Interfaces
{
    public interface IQueueService<T>
    {
        Task EnqueueAsync(T message, CancellationToken cancellationToken = default);
    }
}
