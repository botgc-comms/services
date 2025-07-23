using BOTGC.API.Models;

namespace BOTGC.API.Interfaces
{
    public interface ITeeTimeUsageTaskQueue
    {
        Task QueueTaskAsync(TeeTimeUsageTaskItem taskItem);
        Task<TeeTimeUsageTaskItem> DequeueAsync(CancellationToken cancellationToken);
    }
}