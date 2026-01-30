using System.Text.Json;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.EventBus.Events;
using BOTGC.API.Services.Events;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.EventBus.Subscribers;

public sealed class OrderHandicapCertificateSubscriber(
    ILogger<OrderHandicapCertificateSubscriber> logger,
    ISubscriberQueueFactory queues,
    IOptions<EventPipelineOptions> options,
    JsonSerializerOptions json)
    : SubscriberBase<FirstHandicapAwardedEvent, OrderHandicapCertificateSubscriber>(logger, queues, options, json)
{
    protected override Task ProcessAsync(FirstHandicapAwardedEvent evt, CancellationToken cancellationToken)
    {
        _logger.LogInformation($"Ordering handicap certiciate for member id {evt.MemberId}");

        // TODO Order a handicap certificate.

        return Task.CompletedTask;
    }
}
