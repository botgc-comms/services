using Azure.Storage.Queues;
using BOTGC.API.Common;
using BOTGC.API.Dto;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services
{
    public class StockTakeCompletedQueueService : BaseQueueService<StockTakeCompletedTicketCommandDto>
    {
        public StockTakeCompletedQueueService(IOptions<AppSettings> settings, ILogger<StockTakeCompletedQueueService> logger)
            : base(
                new QueueClient(settings.Value.Queue.ConnectionString, AppConstants.StockTakeCompletedQueueName),
                new QueueClient(settings.Value.Queue.ConnectionString, $"{AppConstants.StockTakeCompletedQueueName}-dlq"),
                logger)
        {
        }
    }
}
