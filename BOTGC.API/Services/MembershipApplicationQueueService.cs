using Azure.Storage.Queues;
using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BOTGC.API.Services
{
    public class MembershipApplicationQueueService : IQueueService<NewMemberApplicationDto>
    {
        private readonly AppSettings _settings;
        private readonly QueueClient _queueClient;
        private readonly ILogger<MembershipApplicationQueueService> _logger;

        public MembershipApplicationQueueService(
            IOptions<AppSettings> settings,
            ILogger<MembershipApplicationQueueService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

            var connectionString = _settings.Queue.ConnectionString;
            var queueName = AppConstants.MembershipApplicationQueueName;

            _queueClient = new QueueClient(connectionString, queueName);
            _queueClient.CreateIfNotExists();
        }

        public async Task EnqueueAsync(NewMemberApplicationDto item, CancellationToken cancellationToken = default)
        {
            try
            {
                var payload = JsonSerializer.Serialize(item);
                await _queueClient.SendMessageAsync(payload);
                _logger.LogInformation("Queued new membership application.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue membership application.");
                throw;
            }
        }
    }
}
