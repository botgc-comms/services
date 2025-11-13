using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using BOTGC.API.Common;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
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

        public async Task EnqueueManyAsync(IEnumerable<T> items, CancellationToken cancellationToken = default)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }

            var list = items as IList<T> ?? items.ToList();
            if (list.Count == 0)
            {
                _logger.LogInformation("EnqueueManyAsync called with no items for type {Type}.", typeof(T).Name);
                return;
            }

            foreach (var item in list)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var payload = JsonSerializer.Serialize(item);
                    await _queueClient.SendMessageAsync(payload, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to enqueue message of type {Type} in EnqueueManyAsync.", typeof(T).Name);
                    throw;
                }
            }

            _logger.LogInformation("Queued {Count} messages of type {Type}.", list.Count, typeof(T).Name);
        }

        public async Task DeadLetterEnqueueAsync(T item, long dequeueCount, DateTime? errorAt = null, Exception? lastError = null, CancellationToken cancellationToken = default)
        {
            try
            {
                var envelope = new DeadLetterEnvelope<T>(
                    item,
                    dequeueCount,
                    errorAt ?? DateTime.UtcNow,
                    lastError
                );

                var json = JsonSerializer.Serialize(envelope);
                await _deadLetterQueueClient.SendMessageAsync(json, cancellationToken);

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
            var messages = await ReceiveMessageAsync(
                _queueClient,
                maxMessages,
                visibilityTimeout ?? TimeSpan.FromMinutes(AppConstants.QueueVisibilityTimeoutMinutes),
                cancellationToken
            );

            return messages.Select(m =>
            {
                T? payload = default;
                var raw = m.MessageText;

                try
                {
                    var envelope = JsonSerializer.Deserialize<DeadLetterEnvelope<T>>(raw);
                    if (envelope != null && envelope.OriginalMessage != null)
                    {
                        payload = envelope.OriginalMessage;
                    }
                    else
                    {
                        var deserialised = JsonSerializer.Deserialize<T>(raw);
                        if (deserialised != null)
                        {
                            payload = deserialised;
                        }
                        else
                        {
                            _logger.LogWarning("Deserialised object of type {Type} was null", typeof(T).Name);
                        }
                    }
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

            _logger.LogInformation(
                "Ensured queue '{QueueName}' and dead-letter queue '{DeadLetterQueueName}' exist.",
                _queueClient.Name,
                _deadLetterQueueClient.Name
            );
        }

        private static async Task<QueueMessage[]> ReceiveMessageAsync(QueueClient client, int maxMessages, TimeSpan visibilityTimeout, CancellationToken cancellationToken)
        {
            return await client.ReceiveMessagesAsync(maxMessages, visibilityTimeout, cancellationToken);
        }
    }
}
