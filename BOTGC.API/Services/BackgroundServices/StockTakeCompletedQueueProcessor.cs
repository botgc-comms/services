using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.BackgroundServices
{
    public sealed class StockTakeCompletedQueueProcessor : BackgroundService
    {
        private readonly AppSettings _settings;
        private readonly ILogger<StockTakeCompletedQueueProcessor> _logger;
        private readonly IMediator _mediator;
        private readonly IQueueService<StockTakeCompletedCommand> _completedQueue;

        public StockTakeCompletedQueueProcessor(
            IOptions<AppSettings> settings,
            ILogger<StockTakeCompletedQueueProcessor> logger,
            IMediator mediator,
            IQueueService<StockTakeCompletedCommand> completedQueue)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _completedQueue = completedQueue ?? throw new ArgumentNullException(nameof(completedQueue));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const int maxAttempts = 5;
            Exception? lastError = null;

            while (!stoppingToken.IsCancellationRequested)
            {
                var messages = await _completedQueue.ReceiveMessagesAsync(5, cancellationToken: stoppingToken);

                foreach (var msg in messages)
                {
                    var payload = msg.Payload;

                    if (payload is null)
                    {
                        _logger.LogWarning("StockTakeCompleted: failed to deserialise message {MessageId}.", msg.Message.MessageId);
                        await _completedQueue.DeleteMessageAsync(msg.Message.MessageId, msg.Message.PopReceipt, stoppingToken);
                        continue;
                    }

                    try
                    {
                        if (msg.Message.DequeueCount > maxAttempts)
                        {
                            _logger.LogWarning("StockTakeCompleted: max attempts exceeded for message {MessageId}. Dead-lettering.", msg.Message.MessageId);
                            await _completedQueue.DeadLetterEnqueueAsync(payload, msg.Message.DequeueCount, DateTime.UtcNow, lastError, stoppingToken);
                            await _completedQueue.DeleteMessageAsync(msg.Message.MessageId, msg.Message.PopReceipt, stoppingToken);
                            continue;
                        }

                        try
                        {
                            _logger.LogInformation(
                                "StockTakeCompleted: posting to Monday for {Date} / {Division}. CorrelationId={CorrelationId}.",
                                payload.Date.ToString("yyyy-MM-dd"),
                                payload.Division,
                                payload.CorrelationId
                            );

                            await _mediator.Send(payload, stoppingToken);

                            await _completedQueue.DeleteMessageAsync(msg.Message.MessageId, msg.Message.PopReceipt, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            lastError = ex;
                            _logger.LogError(
                                ex,
                                "StockTakeCompleted: error posting to Monday for {Date} / {Division}. Attempt {Attempt}.",
                                payload.Date.ToString("yyyy-MM-dd"),
                                payload.Division,
                                msg.Message.DequeueCount
                            );

                            var delaySeconds = Math.Pow(2, Math.Min(msg.Message.DequeueCount, 6));
                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                        _logger.LogError(ex, "StockTakeCompleted: unexpected error handling queue message {MessageId}.", msg.Message.MessageId);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
