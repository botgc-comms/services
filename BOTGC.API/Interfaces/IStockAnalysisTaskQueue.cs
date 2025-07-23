using BOTGC.API.Models;

namespace BOTGC.API.Interfaces
{
    public interface IStockAnalysisTaskQueue
    {
        Task QueueTaskAsync(StockAnalysisTaskItem taskItem);
        Task<StockAnalysisTaskItem?> DequeueAsync(CancellationToken cancellationToken);
    }
}