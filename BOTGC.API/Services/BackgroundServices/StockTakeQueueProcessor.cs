// BOTGC.API/Services/BackgroundServices/StockTakeQueueProcessor.cs
using BOTGC.API.Common;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.BackgroundServices
{
    public sealed class StockTakeQueueProcessor : BackgroundService
    {
        private readonly AppSettings _settings;
        private readonly ILogger<StockTakeQueueProcessor> _logger;
        private readonly IMediator _mediator;
        private readonly IQueueService<ProcessStockTakeCommand> _stocktakeQueueService;

        public StockTakeQueueProcessor(IOptions<AppSettings> settings,
                                       ILogger<StockTakeQueueProcessor> logger,
                                       IMediator mediator,
                                       IQueueService<ProcessStockTakeCommand> stocktakeQueueService)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _stocktakeQueueService = stocktakeQueueService ?? throw new ArgumentNullException(nameof(stocktakeQueueService));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const int maxAttempts = 5;
            Exception? lastError = null;

            while (!stoppingToken.IsCancellationRequested)
            {
                var messages = await _stocktakeQueueService.ReceiveMessagesAsync(5, cancellationToken: stoppingToken);

                foreach (var msg in messages)
                {
                    var payload = msg.Payload;

                    if (payload is null)
                    {
                        _logger.LogWarning("StockTake: failed to deserialise message {MessageId}.", msg.Message.MessageId);
                        await _stocktakeQueueService.DeleteMessageAsync(msg.Message.MessageId, msg.Message.PopReceipt, stoppingToken);
                        continue;
                    }

                    try
                    {
                        if (msg.Message.DequeueCount > maxAttempts)
                        {
                            _logger.LogWarning("StockTake: max attempts exceeded for message {MessageId}. Dead-lettering.", msg.Message.MessageId);
                            await _stocktakeQueueService.DeadLetterEnqueueAsync(payload, msg.Message.DequeueCount, DateTime.UtcNow, lastError, stoppingToken);
                            await _stocktakeQueueService.DeleteMessageAsync(msg.Message.MessageId, msg.Message.PopReceipt, stoppingToken);
                            continue;
                        }

                        try
                        {
                            _logger.LogInformation(
                                "StockTake: processing sheet {Date} for {Division}. CorrelationId={CorrelationId}.",
                                payload.Sheet.Date.ToString("yyyy-MM-dd"),
                                payload.Sheet.Division,
                                payload.CorrelationId
                            );

                            await _mediator.Send(payload, stoppingToken);

                            await _stocktakeQueueService.DeleteMessageAsync(
                                msg.Message.MessageId,
                                msg.Message.PopReceipt,
                                stoppingToken
                            );
                        }
                        catch (Exception ex)
                        {
                            lastError = ex;
                            _logger.LogError(
                                ex,
                                "StockTake: error processing sheet {Date} for {Division}. Attempt {Attempt}.",
                                payload.Sheet.Date.ToString("yyyy-MM-dd"),
                                payload.Sheet.Division,
                                msg.Message.DequeueCount
                            );

                            var delaySeconds = Math.Pow(2, Math.Min(msg.Message.DequeueCount, 6));
                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                        _logger.LogError(ex, "StockTake: unexpected error handling queue message {MessageId}.", msg.Message.MessageId);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}
