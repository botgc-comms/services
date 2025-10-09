using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;


namespace BOTGC.API.Services.BackgroundServices
{
    public class StockWastageQueueProcessor(IOptions<AppSettings> settings, 
                                            ILogger<StockWastageQueueProcessor> logger,
                                            IMediator mediator,
                                            IQueueService<WasteEntryCommandDto> stockWastageQueueService) : BackgroundService
    {
        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<StockWastageQueueProcessor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly IQueueService<WasteEntryCommandDto> _stockWastageQueueService = stockWastageQueueService ?? throw new ArgumentNullException(nameof(stockWastageQueueService));

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            const int maxAttempts = 5;
            Exception? lastError = default;

            while (!stoppingToken.IsCancellationRequested)
            {
                var messages = await _stockWastageQueueService.ReceiveMessagesAsync(maxMessages: 5, cancellationToken: stoppingToken);

                foreach (var msg in messages)
                {
                    var entry = msg.Payload;

                    if (entry == null)
                    {
                        _logger.LogWarning("Failed to deserialise waste entry for message {MessageId}.", msg.Message.MessageId);
                        await _stockWastageQueueService.DeleteMessageAsync(msg.Message.MessageId, msg.Message.PopReceipt, stoppingToken);
                        continue;
                    }

                    try
                    {
                        if (msg.Message.DequeueCount > maxAttempts)
                        {
                            await _stockWastageQueueService.DeadLetterEnqueueAsync(entry, msg.Message.DequeueCount, DateTime.UtcNow, lastError, stoppingToken);
                            await _stockWastageQueueService.DeleteMessageAsync(msg.Message.MessageId, msg.Message.PopReceipt, stoppingToken);
                            continue;
                        }

                        try
                        {
                            var command = new ConfirmAddWastageCommand
                            {
                                WastageDateUtc = entry.WastageDateUtc,
                                ProductId = int.Parse(entry.ProductId.ToString()),
                                StockRoomId = entry.StockRoomId ?? _settings.Waste.DefaultStockRoom,
                                Quantity = entry.Quantity,
                                Reason = entry.Reason
                            };

                            await _mediator.Send(command, stoppingToken);
                            await _stockWastageQueueService.DeleteMessageAsync(msg.Message.MessageId, msg.Message.PopReceipt, stoppingToken);
                        }
                        catch (Exception ex)
                        {
                            lastError = ex;
                            _logger.LogError(ex, "Error confirming wastage for product {ProductId} on {Date}.", entry.ProductId, entry.WastageDateUtc);
                            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, msg.Message.DequeueCount)), stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;
                        _logger.LogError(ex, "Unexpected error processing stock wastage message {MessageId}.", msg.Message.MessageId);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }
}

