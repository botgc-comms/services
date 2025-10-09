using Azure.Storage.Queues;
using BOTGC.API.Common;
using BOTGC.API.Dto;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services
{
    public class StockWastageQueueService : BaseQueueService<WasteEntryCommandDto>
    {
        public StockWastageQueueService(IOptions<AppSettings> settings, ILogger<StockWastageQueueService> logger)
            : base(
                new QueueClient(settings.Value.Queue.ConnectionString, AppConstants.StockWastageQueueName),
                new QueueClient(settings.Value.Queue.ConnectionString, $"{AppConstants.StockWastageQueueName}-dlq"),
                logger)
        {
        }
    }
}
