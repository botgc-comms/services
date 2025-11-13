using BOTGC.API;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

public sealed class PrizeInvoiceQueueProcessor(
        IOptions<AppSettings> settings,
        ILogger<PrizeInvoiceQueueProcessor> logger,
        IMediator mediator,
        IQueueService<ProcessPrizeInvoiceCommand> prizeInvoiceQueueService
    ) : BackgroundService
{
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<PrizeInvoiceQueueProcessor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly IQueueService<ProcessPrizeInvoiceCommand> _prizeInvoiceQueueService = prizeInvoiceQueueService ?? throw new ArgumentNullException(nameof(prizeInvoiceQueueService));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int maxAttempts = 5;
        Exception? lastError = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            var messages = await _prizeInvoiceQueueService.ReceiveMessagesAsync(
                maxMessages: 5,
                visibilityTimeout: null,
                cancellationToken: stoppingToken);

            foreach (var msg in messages)
            {
                var command = msg.Payload;

                if (command == null)
                {
                    _logger.LogWarning(
                        "Failed to deserialise ProcessPrizeInvoiceCommand for message {MessageId}.",
                        msg.Message.MessageId);

                    await _prizeInvoiceQueueService.DeleteMessageAsync(
                        msg.Message.MessageId,
                        msg.Message.PopReceipt,
                        stoppingToken);

                    continue;
                }

                try
                {
                    if (msg.Message.DequeueCount > maxAttempts)
                    {
                        _logger.LogWarning(
                            "Max attempts exceeded for invoice message {MessageId}, CompetitionId {CompetitionId}. Sending to dead-letter queue.",
                            msg.Message.MessageId,
                            command.CompetitionId);

                        await _prizeInvoiceQueueService.DeadLetterEnqueueAsync(
                            command,
                            msg.Message.DequeueCount,
                            DateTime.UtcNow,
                            lastError,
                            stoppingToken);

                        await _prizeInvoiceQueueService.DeleteMessageAsync(
                            msg.Message.MessageId,
                            msg.Message.PopReceipt,
                            stoppingToken);

                        continue;
                    }

                    try
                    {
                        var success = await _mediator.Send(command, stoppingToken);

                        if (success)
                        {
                            _logger.LogInformation(
                                "Successfully processed prize invoice for CompetitionId {CompetitionId}, message {MessageId}.",
                                command.CompetitionId,
                                msg.Message.MessageId);

                            await _prizeInvoiceQueueService.DeleteMessageAsync(
                                msg.Message.MessageId,
                                msg.Message.PopReceipt,
                                stoppingToken);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Handler reported failure for prize invoice message {MessageId}, CompetitionId {CompetitionId}. Retrying.",
                                msg.Message.MessageId,
                                command.CompetitionId);

                            var delaySeconds = Math.Pow(2, msg.Message.DequeueCount);
                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;

                        _logger.LogError(
                            ex,
                            "Error executing ProcessPrizeInvoiceCommand for message {MessageId}, CompetitionId {CompetitionId}.",
                            msg.Message.MessageId,
                            command.CompetitionId);

                        var delaySeconds = Math.Pow(2, msg.Message.DequeueCount);
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex;

                    _logger.LogError(
                        ex,
                        "Unexpected error handling prize invoice queue message {MessageId}.",
                        msg.Message.MessageId);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
