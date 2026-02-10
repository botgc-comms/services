using System.Text.Json;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.EventBus.Detectors;
using BOTGC.API.Services.EventBus.Events;
using BOTGC.API.Services.Events;
using BOTGC.API.Services.Events.Detectors;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.EventBus.Subscribers;

[SubscriberName("parent-learning-trigger")]
public sealed class TriggerParentLearningSubscriber(
    ILogger<TriggerParentLearningSubscriber> logger,
    IMediator mediator,
    ISubscriberQueueFactory queues,
    IOptions<EventPipelineOptions> options,
    JsonSerializerOptions json,
    IMemberDetectorTriggerScheduler triggerScheduler)
    : SubscriberBase<LearningCompletedEvent, TriggerParentLearningSubscriber>(logger, queues, options, json)
{
    private readonly IMemberDetectorTriggerScheduler _triggerScheduler = triggerScheduler ?? throw new ArgumentNullException(nameof(triggerScheduler));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private static readonly string Detector = DetectorName.For(typeof(ParentLearningProjectionDetector));
    private static readonly TimeSpan QuietPeriod = TimeSpan.FromSeconds(5);

    protected async override Task ProcessAsync(LearningCompletedEvent evt, CancellationToken cancellationToken)
    {
        var parents = await _mediator.Send(new GetParentsQuery(), cancellationToken);

        var childIds = parents
            .Where(p => p.ParentMemberId == evt.MemberId)
            .SelectMany(p => p.Children)
            .Distinct()
            .ToArray();

        foreach (var childID in childIds)
        {
            _triggerScheduler.Schedule(
            detectorName: Detector,
            memberId: childID,
            correlationId: evt.EventId.ToString("N"),
            quietPeriod: QuietPeriod,
            stoppingToken: cancellationToken);
        }
        
    }
}