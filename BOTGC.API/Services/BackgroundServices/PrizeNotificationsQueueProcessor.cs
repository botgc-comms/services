using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.BackgroundServices;

public sealed class PrizeNotificationsQueueProcessor(
        IOptions<AppSettings> settings,
        ILogger<PrizeNotificationsQueueProcessor> logger,
        IMediator mediator,
        IQueueService<SendPrizeNotificationEmailCommand> prizeNotificationsQueueService
    ) : BackgroundService
{
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<PrizeNotificationsQueueProcessor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly IQueueService<SendPrizeNotificationEmailCommand> _prizeNotificationsQueueService = prizeNotificationsQueueService ?? throw new ArgumentNullException(nameof(prizeNotificationsQueueService));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int maxAttempts = 5;
        Exception? lastError = default;

        while (!stoppingToken.IsCancellationRequested)
        {
            var messages = await _prizeNotificationsQueueService.ReceiveMessagesAsync(
                maxMessages: 5,
                visibilityTimeout: null,
                cancellationToken: stoppingToken);

            foreach (var msg in messages)
            {
                var command = msg.Payload;

                if (command == null)
                {
                    _logger.LogWarning(
                        "Failed to deserialise SendPrizeNotificationEmailCommand for message {MessageId}.",
                        msg.Message.MessageId);

                    await _prizeNotificationsQueueService.DeleteMessageAsync(
                        msg.Message.MessageId,
                        msg.Message.PopReceipt,
                        stoppingToken);

                    continue;
                }

                try
                {
                    if (msg.Message.DequeueCount > maxAttempts)
                    {
                        await _prizeNotificationsQueueService.DeadLetterEnqueueAsync(
                            command,
                            msg.Message.DequeueCount,
                            DateTime.UtcNow,
                            lastError,
                            stoppingToken);

                        await _prizeNotificationsQueueService.DeleteMessageAsync(
                            msg.Message.MessageId,
                            msg.Message.PopReceipt,
                            stoppingToken);

                        continue;
                    }

                    try
                    {
                        var sent = await _mediator.Send(command, stoppingToken);

                        if (sent)
                        {
                            _logger.LogInformation(
                                "Successfully processed prize notification for PlayerId {PlayerId}, Competition {Competition}.",
                                command.PlayerId,
                                command.CompetitionName);

                            await _prizeNotificationsQueueService.DeleteMessageAsync(
                                msg.Message.MessageId,
                                msg.Message.PopReceipt,
                                stoppingToken);
                        }
                        else
                        {
                            _logger.LogWarning(
                                "Handler reported failure for prize notification message {MessageId}.",
                                msg.Message.MessageId);

                            var delaySeconds = Math.Pow(2, msg.Message.DequeueCount);
                            await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                        }
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;

                        _logger.LogError(
                            ex,
                            "Error executing SendPrizeNotificationEmailCommand for message {MessageId}.",
                            msg.Message.MessageId);

                        var delaySeconds = Math.Pow(2, msg.Message.DequeueCount);
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex;

                    _logger.LogError(
                        ex,
                        "Unexpected error handling prize notification queue message {MessageId}.",
                        msg.Message.MessageId);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
