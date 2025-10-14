using Azure.Storage.Queues;
using BOTGC.API.Common;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services
{
    public class StockTakeQueueService : BaseQueueService<ProcessStockTakeCommand>
    {
        public StockTakeQueueService(IOptions<AppSettings> settings, ILogger<StockTakeQueueService> logger)
            : base(
                new QueueClient(settings.Value.Queue.ConnectionString, AppConstants.StockTakeProcessQueueName),
                new QueueClient(settings.Value.Queue.ConnectionString, $"{AppConstants.StockTakeProcessQueueName}-dlq"),
                logger)
        {
        }
    }
}
