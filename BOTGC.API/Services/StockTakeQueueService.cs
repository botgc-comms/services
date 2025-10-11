using Azure.Storage.Queues;
using BOTGC.API.Common;
using BOTGC.API.Dto;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services
{
    public class StockTakeQueueService : BaseQueueService<StockTakeSheetProcessCommandDto>
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
