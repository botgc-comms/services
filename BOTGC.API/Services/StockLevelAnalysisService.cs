using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using System.Collections.Concurrent;

namespace BOTGC.API.Services
{
    public class StockLevelAnalysisTaskQueue : IStockAnalysisTaskQueue
    {
        private readonly ILogger<StockLevelAnalysisTaskQueue> _logger;
        private readonly ConcurrentQueue<StockAnalysisTaskItem> _queue = new();
        private readonly SemaphoreSlim _signal = new(0);

        public StockLevelAnalysisTaskQueue(ILogger<StockLevelAnalysisTaskQueue> logger)
        {
            _logger = logger;
        }

        public Task QueueTaskAsync(StockAnalysisTaskItem taskItem)
        {
            _queue.Enqueue(taskItem);
            _signal.Release();
            _logger.LogInformation("Queued Stock Analysis task for {RequestedAt}", taskItem.RequestedAt);
            return Task.CompletedTask;
        }

        public async Task<StockAnalysisTaskItem?> DequeueAsync(CancellationToken cancellationToken)
        {
            await _signal.WaitAsync(cancellationToken);
            if (_queue.TryDequeue(out var task))
            {
                return task;
            }
            return null;
        }
    }
}
