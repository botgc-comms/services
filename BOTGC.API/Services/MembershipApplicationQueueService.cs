using Azure.Storage.Queues;
using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BOTGC.API.Services
{
    public class MembershipApplicationQueueService : IQueueService<NewMemberApplicationDto>, IQueueService<NewMemberApplicationResultDto>, IQueueService<NewMemberPropertyUpdateDto>
    {
        private readonly AppSettings _settings;
        private readonly QueueClient _newApplicationQueueClient;
        private readonly QueueClient _newMemberAddedQueueClient;
        private readonly QueueClient _memberPropertyUpdateQueueClient;
        private readonly ILogger<MembershipApplicationQueueService> _logger;

        public MembershipApplicationQueueService(
            IOptions<AppSettings> settings,
            ILogger<MembershipApplicationQueueService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));

            var connectionString = _settings.Queue.ConnectionString;

            _newApplicationQueueClient = new QueueClient(connectionString, AppConstants.MembershipApplicationQueueName);
            _newApplicationQueueClient.CreateIfNotExists();

            _newMemberAddedQueueClient = new QueueClient(connectionString, AppConstants.NewMemberAddedQueueName);
            _newMemberAddedQueueClient.CreateIfNotExists();

            _memberPropertyUpdateQueueClient = new QueueClient(connectionString, AppConstants.MemberPropertyUpdateQueueName);
            _memberPropertyUpdateQueueClient.CreateIfNotExists();
        }

        public async Task EnqueueAsync(NewMemberApplicationDto item, CancellationToken cancellationToken = default)
        {
            try
            {
                var payload = JsonSerializer.Serialize(item);
                await _newApplicationQueueClient.SendMessageAsync(payload);
                _logger.LogInformation("Queued new membership application event.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue membership application event.");
                throw;
            }
        }

        public async Task EnqueueAsync(NewMemberApplicationResultDto item, CancellationToken cancellationToken = default)
        {
            try
            {
                var payload = JsonSerializer.Serialize(item);
                await _newMemberAddedQueueClient.SendMessageAsync(payload);
                _logger.LogInformation("Queued new member added event.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue new member added event.");
                throw;
            }
        }

        public async Task EnqueueAsync(NewMemberPropertyUpdateDto item, CancellationToken cancellationToken = default)
        {
            try
            {
                var payload = JsonSerializer.Serialize(item);
                await _newMemberAddedQueueClient.SendMessageAsync(payload);
                _logger.LogInformation("Queued new member property update event.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue new member property update event.");
                throw;
            }
        }
    }
}
