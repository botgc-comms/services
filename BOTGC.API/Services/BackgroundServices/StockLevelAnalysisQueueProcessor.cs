using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Queues.Models;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BOTGC.API.Services.BackgroundServices
{
    public class StockLevelAnalysisQueueProcessor : BackgroundService
    {
        private readonly ILogger<StockLevelAnalysisQueueProcessor> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IStockAnalysisTaskQueue _taskQueue;
        private readonly IDistributedLockManager _distributedLockManager;

        public StockLevelAnalysisQueueProcessor(
            ILogger<StockLevelAnalysisQueueProcessor> logger,
            IServiceScopeFactory serviceScopeFactory,
            IStockAnalysisTaskQueue taskQueue,
            IDistributedLockManager distributedLockManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
            _distributedLockManager = distributedLockManager ?? throw new ArgumentNullException(nameof(distributedLockManager));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("StockLevelAnalysisQueueProcessor started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await using (var distLock = await _distributedLockManager.AcquireLockAsync("stock-level-analysis", expiry: TimeSpan.FromMinutes(5), cancellationToken: stoppingToken))
                    {
                        var taskItem = await _taskQueue.DequeueAsync(stoppingToken);

                        if (distLock.IsAcquired)
                        {
                            if (taskItem != null)
                            {
                                using var scope = _serviceScopeFactory.CreateScope();
                                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                                var taskBoardService = scope.ServiceProvider.GetRequiredService<ITaskBoardService>();

                                var query = new GetStockLevelsQuery();
                                var allStock = await mediator.Send(query, stoppingToken);

                                // Filter: only items with status "Running Low" or "Very Low"
                                var ofConcern = allStock
                                    .Where(dto =>
                                        (dto.MinAlert.HasValue && dto.MinAlert.Value > 0 && dto.TotalQuantity.HasValue && dto.TotalQuantity <= dto.MinAlert) ||
                                        (dto.MaxAlert.HasValue && dto.MaxAlert.Value > 0 && dto.TotalQuantity.HasValue && dto.TotalQuantity <= dto.MaxAlert) ||
                                        (dto.TotalQuantity.HasValue && dto.TotalQuantity <= 0))
                                    .Where(dto => dto.IsActive ?? false)
                                    .ToList();

                                await taskBoardService.SyncStockLevelsAsync(ofConcern);

                                _logger.LogInformation("Processed stock analysis task at {Time}. Concern items: {Count}", DateTime.UtcNow, ofConcern.Count);
                            }
                            else
                            {
                                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                            }
                        }
                        else
                        {
                            // Could not acquire lock, so delete the message
                            _logger.LogInformation("Skipped stock analysis task because lock not acquired. Message deleted from queue.");
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing stock analysis task.");
                    await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
                }
            }
            _logger.LogInformation("StockLevelAnalysisQueueProcessor stopped.");
        }
    }
}
