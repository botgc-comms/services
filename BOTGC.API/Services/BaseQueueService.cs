using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using BOTGC.API.Common;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using Microsoft.Extensions.Logging;
using System.Net.Sockets;
using System.Text.Json;

namespace BOTGC.API.Services
{
    public abstract class BaseQueueService<T> : IQueueService<T>
    {
        private readonly QueueClient _queueClient;
        private readonly QueueClient _deadLetterQueueClient;
        private readonly ILogger _logger;

        private readonly SemaphoreSlim _initLock;
        private Task? _initTask;

        protected BaseQueueService(QueueClient queueClient, QueueClient deadLetterQueueClient, ILogger logger)
        {
            _queueClient = queueClient ?? throw new ArgumentNullException(nameof(queueClient));
            _deadLetterQueueClient = deadLetterQueueClient ?? throw new ArgumentNullException(nameof(deadLetterQueueClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _initLock = new SemaphoreSlim(1, 1);
        }

        public async Task EnqueueAsync(T item, CancellationToken cancellationToken = default)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            await EnsureInitialisedAsync(cancellationToken);

            var payload = JsonSerializer.Serialize(item);

            await ExecuteWithRetriesAsync(
                operationName: "SendMessage",
                operation: ct => _queueClient.SendMessageAsync(payload, ct),
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("Queued message of type {Type}.", typeof(T).Name);
        }

        public async Task EnqueueManyAsync(IEnumerable<T> items, CancellationToken cancellationToken = default)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));

            await EnsureInitialisedAsync(cancellationToken);

            var list = items as IList<T> ?? items.ToList();
            if (list.Count == 0)
            {
                _logger.LogInformation("EnqueueManyAsync called with no items for type {Type}.", typeof(T).Name);
                return;
            }

            var queued = 0;

            foreach (var item in list)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (item == null)
                {
                    _logger.LogWarning("Null item encountered in EnqueueManyAsync for type {Type}. Skipping.", typeof(T).Name);
                    continue;
                }

                var payload = JsonSerializer.Serialize(item);

                await ExecuteWithRetriesAsync(
                    operationName: "SendMessage",
                    operation: ct => _queueClient.SendMessageAsync(payload, ct),
                    cancellationToken: cancellationToken
                );

                queued++;
            }

            _logger.LogInformation("Queued {Count} messages of type {Type}.", queued, typeof(T).Name);
        }

        public async Task DeadLetterEnqueueAsync(T item, long dequeueCount, DateTime? errorAt = null, Exception? lastError = null, CancellationToken cancellationToken = default)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));

            await EnsureInitialisedAsync(cancellationToken);

            var envelope = new DeadLetterEnvelope<T>(
                item,
                dequeueCount,
                errorAt ?? DateTime.UtcNow,
                lastError
            );

            var json = JsonSerializer.Serialize(envelope);

            await ExecuteWithRetriesAsync(
                operationName: "DeadLetterSendMessage",
                operation: ct => _deadLetterQueueClient.SendMessageAsync(json, ct),
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("Queued dead-letter message of type {Type}.", typeof(T).Name);
        }

        public async Task<IQueueMessage<T>[]> ReceiveMessagesAsync(int maxMessages, TimeSpan? visibilityTimeout, CancellationToken cancellationToken = default)
        {
            await EnsureInitialisedAsync(cancellationToken);

            var messages = await ExecuteWithRetriesAsync(
                operationName: "ReceiveMessages",
                operation: ct => _queueClient.ReceiveMessagesAsync(
                    maxMessages,
                    visibilityTimeout ?? TimeSpan.FromMinutes(AppConstants.QueueVisibilityTimeoutMinutes),
                    ct
                ),
                cancellationToken: cancellationToken
            );

            return messages.Value.Select(m =>
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
                        payload = JsonSerializer.Deserialize<T>(raw);
                        if (payload == null)
                        {
                            _logger.LogWarning("Deserialised object of type {Type} was null.", typeof(T).Name);
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
            if (string.IsNullOrWhiteSpace(messageId)) throw new ArgumentException("MessageId is required.", nameof(messageId));
            if (string.IsNullOrWhiteSpace(popReceipt)) throw new ArgumentException("PopReceipt is required.", nameof(popReceipt));

            await EnsureInitialisedAsync(cancellationToken);

            await ExecuteWithRetriesAsync(
                operationName: "DeleteMessage",
                operation: ct => _queueClient.DeleteMessageAsync(messageId, popReceipt, ct),
                cancellationToken: cancellationToken
            );

            _logger.LogInformation("Deleted message {MessageId} of type {Type}.", messageId, typeof(T).Name);
        }

        private async Task EnsureInitialisedAsync(CancellationToken cancellationToken)
        {
            var existing = Volatile.Read(ref _initTask);
            if (existing != null)
            {
                await existing;
                return;
            }

            await _initLock.WaitAsync(cancellationToken);
            try
            {
                if (_initTask == null)
                {
                    _initTask = InitialiseAsync(cancellationToken);
                    Volatile.Write(ref _initTask, _initTask);
                }
            }
            finally
            {
                _initLock.Release();
            }

            await _initTask;
        }

        private async Task InitialiseAsync(CancellationToken cancellationToken)
        {
            await ExecuteWithRetriesAsync(
                operationName: "CreateQueueIfNotExists",
                operation: ct => _queueClient.CreateIfNotExistsAsync(cancellationToken: ct),
                cancellationToken: cancellationToken
            );

            await ExecuteWithRetriesAsync(
                operationName: "CreateDeadLetterQueueIfNotExists",
                operation: ct => _deadLetterQueueClient.CreateIfNotExistsAsync(cancellationToken: ct),
                cancellationToken: cancellationToken
            );

            _logger.LogInformation(
                "Ensured queue '{QueueName}' and dead-letter queue '{DeadLetterQueueName}' exist.",
                _queueClient.Name,
                _deadLetterQueueClient.Name
            );
        }

        private async Task ExecuteWithRetriesAsync(string operationName, Func<CancellationToken, Task> operation, CancellationToken cancellationToken)
        {
            await ExecuteWithRetriesAsync<object?>(
                operationName,
                async ct =>
                {
                    await operation(ct);
                    return null;
                },
                cancellationToken
            );
        }

        private async Task<TOut> ExecuteWithRetriesAsync<TOut>(string operationName, Func<CancellationToken, Task<TOut>> operation, CancellationToken cancellationToken)
        {
            const int maxAttempts = 5;

            var attempt = 0;
            Exception? last = null;

            while (attempt < maxAttempts)
            {
                cancellationToken.ThrowIfCancellationRequested();
                attempt++;

                try
                {
                    return await operation(cancellationToken);
                }
                catch (Exception ex) when (IsTransient(ex))
                {
                    last = ex;

                    if (attempt >= maxAttempts)
                    {
                        _logger.LogError(ex, "{Operation} failed after {Attempts} attempts for queue {QueueName}.", operationName, attempt, _queueClient.Name);
                        throw;
                    }

                    var delay = ComputeDelay(attempt);

                    _logger.LogWarning(
                        ex,
                        "{Operation} transient failure (attempt {Attempt}/{MaxAttempts}) for queue {QueueName}. Retrying in {DelayMs}ms.",
                        operationName,
                        attempt,
                        maxAttempts,
                        _queueClient.Name,
                        (int)delay.TotalMilliseconds
                    );

                    await Task.Delay(delay, cancellationToken);
                }
            }

            throw last ?? new InvalidOperationException($"{operationName} failed with no captured exception.");
        }

        private static TimeSpan ComputeDelay(int attempt)
        {
            var baseMs = Math.Min(10_000, (int)(250 * Math.Pow(2, attempt - 1)));
            var jitterMs = Random.Shared.Next(0, 250);
            return TimeSpan.FromMilliseconds(baseMs + jitterMs);
        }

        private static bool IsTransient(Exception ex)
        {
            if (ex is OperationCanceledException)
            {
                return false;
            }

            if (ex is RequestFailedException rfe)
            {
                if (rfe.Status == 408 || rfe.Status == 429) return true;
                if (rfe.Status >= 500 && rfe.Status <= 599) return true;
            }

            if (ex is HttpRequestException) return true;
            if (ex is IOException) return true;
            if (ex is SocketException) return true;

            if (ex.InnerException != null)
            {
                return IsTransient(ex.InnerException);
            }

            return false;
        }
    }
}
