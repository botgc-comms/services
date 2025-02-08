using Services.Interfaces;
using Services.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Services.Services.BackgroundServices
{
    /// <summary>
    /// Service for handling trophy data retrieval and processing.
    /// </summary>
    public class CompetitionBackgroundService : BackgroundService
    {
        private readonly ICompetitionTaskQueue _taskQueue;
        private readonly ICompetitionProcessorResolver _processorResolver;
        private readonly ILogger<CompetitionBackgroundService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrophyService"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="trophyDataStore">Data store for retrieving trophies.</param>
        public CompetitionBackgroundService(ILogger<CompetitionBackgroundService> logger, ICompetitionTaskQueue taskQueue, ICompetitionProcessorResolver processorResolver)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
            _processorResolver = processorResolver ?? throw new ArgumentNullException(nameof(processorResolver));
        }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var taskItem = await _taskQueue.DequeueAsync(stoppingToken);
                try
                {
                    await ProcessTaskAsync(taskItem, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing task.");
                }
            }
        }

        private async Task ProcessTaskAsync(CompetitionTaskItem taskItem, CancellationToken stoppingToken)
        {
            // Resolve the appropriate processor based on the competition type
            var processor = _processorResolver.GetProcessor(taskItem.CompetitionType);

            // Process the competition using the resolved processor
            await processor.ProcessCompetitionAsync(taskItem.FromDate, taskItem.ToDate, stoppingToken);
        }
    }
}

