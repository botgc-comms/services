using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.BackgroundServices;

public sealed class CompetitionResultsPageUpdateQueueProcessor(
        IOptions<AppSettings> settings,
        ILogger<CompetitionResultsPageUpdateQueueProcessor> logger,
        IMediator mediator,
        IQueueService<ProcessCompetitionWinningsBatchCompletedCommand> competitionResultsQueueService
    ) : BackgroundService
{
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<CompetitionResultsPageUpdateQueueProcessor> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly IQueueService<ProcessCompetitionWinningsBatchCompletedCommand> _competitionResultsQueueService =
        competitionResultsQueueService ?? throw new ArgumentNullException(nameof(competitionResultsQueueService));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        const int maxAttempts = 5;
        Exception? lastError = null;

        while (!stoppingToken.IsCancellationRequested)
        {
            var messages = await _competitionResultsQueueService.ReceiveMessagesAsync(
                maxMessages: 5,
                visibilityTimeout: null,
                cancellationToken: stoppingToken);

            foreach (var msg in messages)
            {
                var payload = msg.Payload;

                if (payload == null)
                {
                    _logger.LogWarning(
                        "Null payload when processing competition winnings batch message {MessageId}. Deleting.",
                        msg.Message.MessageId);

                    await _competitionResultsQueueService.DeleteMessageAsync(
                        msg.Message.MessageId,
                        msg.Message.PopReceipt,
                        stoppingToken);

                    continue;
                }

                try
                {
                    if (msg.Message.DequeueCount > maxAttempts)
                    {
                        _logger.LogError(
                            lastError,
                            "Max dequeue attempts exceeded for message {MessageId}. Sending to dead-letter queue.",
                            msg.Message.MessageId);

                        await _competitionResultsQueueService.DeadLetterEnqueueAsync(
                            payload,
                            msg.Message.DequeueCount,
                            DateTime.UtcNow,
                            lastError,
                            stoppingToken);

                        await _competitionResultsQueueService.DeleteMessageAsync(
                            msg.Message.MessageId,
                            msg.Message.PopReceipt,
                            stoppingToken);

                        continue;
                    }

                    try
                    {
                        var ids = payload.CompetitionIds?.Distinct().ToArray() ?? Array.Empty<int>();
                        if (ids.Length == 0)
                        {
                            _logger.LogWarning(
                                "No CompetitionIds found in winnings batch payload for message {MessageId}. Deleting.",
                                msg.Message.MessageId);

                            await _competitionResultsQueueService.DeleteMessageAsync(
                                msg.Message.MessageId,
                                msg.Message.PopReceipt,
                                stoppingToken);

                            continue;
                        }

                        _logger.LogInformation(
                            "Processing winnings batch completion for CompetitionIds [{CompetitionIds}] calculated on {CalculatedOn}.",
                            string.Join(",", ids),
                            payload.CalculatedOn);

                        // 1. Generate/update winners pages for each competition in this batch.
                        foreach (var competitionId in ids)
                        {
                            if (competitionId <= 0) continue;

                            try
                            {
                                await _mediator.Send(
                                    new GenerateCompetitionWinnersHtmlPageCommand(competitionId),
                                    stoppingToken);
                            }
                            catch (Exception ex)
                            {
                                lastError = ex;

                                _logger.LogError(
                                    ex,
                                    "Failed to generate winners HTML page for CompetitionId {CompetitionId}. " +
                                    "This competition will not have a winners link until retried.",
                                    competitionId);

                                // Do not rethrow here: one failed child page shouldn't prevent
                                // the overall page refresh or other competitions.
                            }
                        }

                        // 2. Rebuild the public competition results page (parent index).
                        await _mediator.Send(new UpdateCompetitionResultsPageCommand(), stoppingToken);

                        // 3. Done with this message.
                        await _competitionResultsQueueService.DeleteMessageAsync(
                            msg.Message.MessageId,
                            msg.Message.PopReceipt,
                            stoppingToken);

                        _logger.LogInformation(
                            "Successfully processed winnings batch completion for CompetitionIds [{CompetitionIds}].",
                            string.Join(",", ids));
                    }
                    catch (Exception ex)
                    {
                        lastError = ex;

                        _logger.LogError(
                            ex,
                            "Error handling competition winnings batch message {MessageId}. Will retry.",
                            msg.Message.MessageId);

                        // simple exponential backoff based on dequeue count
                        var delaySeconds = Math.Pow(2, msg.Message.DequeueCount);
                        await Task.Delay(TimeSpan.FromSeconds(delaySeconds), stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    lastError = ex;

                    _logger.LogError(
                        ex,
                        "Unexpected error processing competition results update message {MessageId}.",
                        msg.Message.MessageId);
                }
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
