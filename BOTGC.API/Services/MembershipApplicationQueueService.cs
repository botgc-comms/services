using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BOTGC.API.Services
{
    public abstract class BaseQueueService<T> : IQueueService<T>
    {
        private readonly QueueClient _queueClient;
        private readonly QueueClient _deadLetterQueueClient;
        private readonly ILogger _logger;

        protected BaseQueueService(QueueClient queueClient, QueueClient deadLetterQueueClient, ILogger logger)
        {
            _queueClient = queueClient;
            _deadLetterQueueClient = deadLetterQueueClient;
            _logger = logger;

            Task.Run(() => EnsureQueuesExistAsync());
        }

        public async Task EnqueueAsync(T item, CancellationToken cancellationToken = default)
        {
            try
            {
                var payload = JsonSerializer.Serialize(item);
                await _queueClient.SendMessageAsync(payload, cancellationToken);
                _logger.LogInformation("Queued message of type {Type}.", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue message of type {Type}.", typeof(T).Name);
                throw;
            }
        }

        public async Task DeadLetterEnqueueAsync(T item, long dequeueCount, DateTime? errorAt = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var deadLetterPayload = new
                {
                    OriginalMessage = item,
                    DequeueCount = dequeueCount,
                    FailedAt = errorAt ?? DateTime.UtcNow
                };

                var deadLetterJson = JsonSerializer.Serialize(deadLetterPayload);

                await _deadLetterQueueClient.SendMessageAsync(deadLetterJson, cancellationToken);
                _logger.LogInformation("Queued dead-letter message of type {Type}.", typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to enqueue dead-letter message of type {Type}.", typeof(T).Name);
                throw;
            }
        }

        public async Task<IQueueMessage<T>[]> ReceiveMessagesAsync(int maxMessages, TimeSpan? visibilityTimeout, CancellationToken cancellationToken = default)
        {
            var messages = await ReceiveMessageAsync(_queueClient, maxMessages, visibilityTimeout ?? TimeSpan.FromMinutes(AppConstants.QueueVisibilityTimeoutMinutes), cancellationToken);

            return messages.Select(m =>
            {
                T? payload = default;
                try
                {
                    payload = JsonSerializer.Deserialize<T>(m.MessageText);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to deserialize message of type {Type}.", typeof(T).Name);
                }

                return new QueueMessageWrapper<T>(m, payload);
            }).ToArray();
        }

        public async Task DeleteMessageAsync(string messageId, string popReceipt, CancellationToken cancellationToken = default)
        {
            try
            {
                await _queueClient.DeleteMessageAsync(messageId, popReceipt, cancellationToken);
                _logger.LogInformation("Deleted message {MessageId} of type {Type}.", messageId, typeof(T).Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete message {MessageId} of type {Type}.", messageId, typeof(T).Name);
                throw;
            }
        }

        private async Task EnsureQueuesExistAsync(CancellationToken cancellationToken = default)
        {
            await _queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);
            await _deadLetterQueueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

            _logger.LogInformation("Ensured queue '{QueueName}' and dead-letter queue '{DeadLetterQueueName}' exist.",
                _queueClient.Name, _deadLetterQueueClient.Name);
        }

        private static async Task<QueueMessage[]> ReceiveMessageAsync(QueueClient client, int maxMessages, TimeSpan visibilityTimeout, CancellationToken cancellationToken)
        {
            return await client.ReceiveMessagesAsync(maxMessages, visibilityTimeout, cancellationToken);
        }
    }

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
