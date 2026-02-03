using System.Text.Json;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.EventBus.Events;
using BOTGC.API.Services.Events;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;
using NUglify.Helpers;

namespace BOTGC.API.Services.EventBus.Subscribers;

[SubscriberName("learning-parent-milestone")]
public sealed class ParentLearningMilestoneSubscriber(
    ILogger<ParentLearningMilestoneSubscriber> logger,
    ISubscriberQueueFactory queues,
    IOptions<EventPipelineOptions> options,
    JsonSerializerOptions json,
    IMediator mediator,
    IEventPublisher publisher)
    : SubscriberBase<LearningCompletedEvent, ParentLearningMilestoneSubscriber>(logger, queues, options, json)
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly IEventPublisher _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));

    protected override async Task ProcessAsync(LearningCompletedEvent evt, CancellationToken cancellationToken)
    {
        var parents = await _mediator.Send(new GetParentsQuery(), cancellationToken);

        var parent = parents.FirstOrDefault(p => p.ParentMemberId == evt.MemberId);
        if (parent == null)
        {
            return;
        }

        var completed = await _mediator.Send(
            new GetCompletedLearningPacksQuery(parent.ParentMemberId),
            cancellationToken);

        var completedSet = completed
            .Select(x => x.PackId)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (completedSet.Count == 0)
        {
            return;
        }

        var occurredAtUtc = DateTimeOffset.UtcNow;

        var childNumbers = parent.Children.ToArray();

        if (childNumbers.Length == 0)
        {
            return;
        }

        foreach (var childMemberNumber in childNumbers)
        {
            var mandatory = await _mediator.Send(
                new GetMandatoryLearningPacksForChildQuery(childMemberNumber),
                cancellationToken);

            if (mandatory.MandatoryPackIds.Count == 0)
            {
                continue;
            }

            var allDone = mandatory.MandatoryPackIds.All(completedSet.Contains);
            if (!allDone)
            {
                continue;
            }

            await _publisher.PublishAsync(new ParentLearningMilestoneAchievedEvent
            {
                MemberId = childMemberNumber,
                ParentMemberId = parent.ParentMemberId,
                ChildCategory = mandatory.ChildCategory,
                OccurredAtUtc = occurredAtUtc
            }, cancellationToken);
        }
    }
}
