using Azure.Storage.Queues;
using BOTGC.API.Common;
using BOTGC.API.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;

namespace BOTGC.API.Services
{
    public class MembershipApplicationQueueService : BaseQueueService<NewMemberApplicationDto>
    {
        public MembershipApplicationQueueService(IOptions<AppSettings> settings, ILogger<MembershipApplicationQueueService> logger)
            : base(
                new QueueClient(settings.Value.Queue.ConnectionString, AppConstants.MembershipApplicationQueueName),
                new QueueClient(settings.Value.Queue.ConnectionString, $"{AppConstants.MembershipApplicationQueueName}-dlq"),
                logger)
        {
        }
    }

    public class NewMemberAddedQueueService : BaseQueueService<NewMemberApplicationResultDto>
    {
        public NewMemberAddedQueueService(IOptions<AppSettings> settings, ILogger<NewMemberAddedQueueService> logger)
            : base(
                new QueueClient(settings.Value.Queue.ConnectionString, AppConstants.NewMemberAddedQueueName),
                new QueueClient(settings.Value.Queue.ConnectionString, $"{AppConstants.NewMemberAddedQueueName}-dlq"),
                logger)
        {
        }
    }

    public class MemberPropertyUpdateQueueService : BaseQueueService<NewMemberPropertyUpdateDto>
    {
        public MemberPropertyUpdateQueueService(IOptions<AppSettings> settings, ILogger<MemberPropertyUpdateQueueService> logger)
            : base(
                new QueueClient(settings.Value.Queue.ConnectionString, AppConstants.MemberPropertyUpdateQueueName),
                new QueueClient(settings.Value.Queue.ConnectionString, $"{AppConstants.MemberPropertyUpdateQueueName}-dlq"),
                logger)
        {
        }
    }
}
