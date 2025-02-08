using Services.Models;

namespace Services.Interfaces
{
    public interface ICompetitionTaskQueue
    {
        Task QueueTaskAsync(CompetitionTaskItem taskItem);
        Task<CompetitionTaskItem> DequeueAsync(CancellationToken cancellationToken);
    }
}