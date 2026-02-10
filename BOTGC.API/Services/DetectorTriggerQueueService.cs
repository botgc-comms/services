using Azure.Storage.Queues;
using BOTGC.API.Common;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services;

public sealed class DetectorTriggerQueueService : BaseQueueService<DetectorTriggerCommand>
{
    public DetectorTriggerQueueService(IOptions<AppSettings> settings, ILogger<DetectorTriggerQueueService> logger)
        : base(
            new QueueClient(settings.Value.Queue.ConnectionString, AppConstants.DetectorTriggerQueueName),
            new QueueClient(settings.Value.Queue.ConnectionString, $"{AppConstants.DetectorTriggerQueueName}-dlq"),
            logger)
    {
    }
}
