using System.Text.Json;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.EventBus.Events;
using BOTGC.API.Services.Events;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.EventBus.Subscribers;

[SubscriberName("rewards-completedquiz")]
public sealed class RewardForCompletedQuizSubscriber(
    ILogger<RewardForCompletedQuizSubscriber> logger,
    ISubscriberQueueFactory queues,
    IOptions<EventPipelineOptions> options,
    JsonSerializerOptions json)
    : SubscriberBase<QuizPassedEvent, RewardForCompletedQuizSubscriber>(logger, queues, options, json)
{
    protected override Task ProcessAsync(QuizPassedEvent evt, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Considering whether to reward member for passing another Quiz {evt.MemberId}");

        // TODO Apply logic to determine when to award voucher.

        return Task.CompletedTask;
    }
}
