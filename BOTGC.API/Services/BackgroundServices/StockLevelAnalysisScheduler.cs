using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;

namespace BOTGC.API.Services.BackgroundServices;

public class StockLevelEnqueueScheduler : BackgroundService
{
    private readonly ILogger<StockLevelEnqueueScheduler> _logger;
    private readonly IStockAnalysisTaskQueue _taskQueue;

    public StockLevelEnqueueScheduler(
        ILogger<StockLevelEnqueueScheduler> logger,
        IStockAnalysisTaskQueue taskQueue)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("StockLevelEnqueueScheduler started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            if (now.Hour >= 7 && now.Hour <= 1 + 24) // 7am to 2am next day
            {
                var taskItem = new StockAnalysisTaskItem
                {
                    RequestedAt = DateTime.UtcNow
                };

                await _taskQueue.QueueTaskAsync(taskItem);
                _logger.LogInformation("Enqueued StockAnalysisTask at {Now}", now);
            }
            else
            {
                _logger.LogInformation("Current time {Now} is outside scheduled sync window.", now);
            }

            // Sleep until the next hour (adjust for drift)
            var nextHour = now.AddHours(1);
            var delay = nextHour.Date.AddHours(nextHour.Hour) - DateTime.Now;
            if (delay < TimeSpan.FromMinutes(1)) // just in case
                delay = TimeSpan.FromMinutes(1);

            await Task.Delay(delay, stoppingToken);
        }
    }
}
