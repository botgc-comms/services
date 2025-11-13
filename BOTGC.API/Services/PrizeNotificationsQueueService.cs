using Azure.Storage.Queues;
using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services
{
    public class PrizeNotificationsQueueService : BaseQueueService<SendPrizeNotificationEmailCommand>
    {
        public PrizeNotificationsQueueService(IOptions<AppSettings> settings, ILogger<PrizeNotificationsQueueService> logger)
            : base(
                new QueueClient(settings.Value.Queue.ConnectionString, AppConstants.PrizeNotificationQueueName),
                new QueueClient(settings.Value.Queue.ConnectionString, $"{AppConstants.PrizeNotificationQueueName}-dlq"),
                logger)
        {
        }
    }

    public sealed class PrizeInvoiceQueueService : BaseQueueService<ProcessPrizeInvoiceCommand>
    {
        public PrizeInvoiceQueueService(IOptions<AppSettings> settings, ILogger<PrizeInvoiceQueueService> logger)
            : base(
                new QueueClient(settings.Value.Queue.ConnectionString, AppConstants.PrizeInvoiceQueueName),
                new QueueClient(settings.Value.Queue.ConnectionString, $"{AppConstants.PrizeInvoiceQueueName}-dlq"),
                logger)
        {
        }
    }

    public class NewCompetitionPrizesCalcualtedQueueService : BaseQueueService<ProcessCompetitionWinningsBatchCompletedCommand>
    {

        public NewCompetitionPrizesCalcualtedQueueService(IOptions<AppSettings> settings, ILogger<NewCompetitionPrizesCalcualtedQueueService> logger)
            : base(
                new QueueClient(settings.Value.Queue.ConnectionString, AppConstants.NewCompetitionPrizesCalcualtedQueueName),
                new QueueClient(settings.Value.Queue.ConnectionString, $"{AppConstants.NewCompetitionPrizesCalcualtedQueueName}-dlq"),
                logger)
        {
        }
    }
}
