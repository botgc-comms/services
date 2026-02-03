using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.EventBus;
using BOTGC.API.Services.EventBus.Events;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.Events.Detectors;

public sealed class LearningCompletionDetectorState
{
    public DateTimeOffset LastProcessedCompletedAtUtc { get; set; } = DateTimeOffset.MinValue;
}


[DetectorSchedule("0 21 * * *", runOnStartup: true)]
public sealed class LearningCompletionDetector(
    IDetectorStateStore stateStore,
    IEventPublisher publisher,
    IMediator mediator,
    IDistributedLockManager lockManager,
    IOptions<AppSettings> appSettings,
    ILogger<LearningCompletionDetector> logger)
    : MemberDetectorBase<LearningCompletionDetectorState>(stateStore, publisher, mediator, lockManager, appSettings, logger)
{
    public override string Name => "learning-completion-detector";

    protected override async Task DetectAsync(
        MemberScope scopeKey,
        DetectorState<LearningCompletionDetectorState> state,
        IDetectorEventSink events,
        CancellationToken cancellationToken)
    {
        var memberId = scopeKey.MemberNumber;

        var detectorState = state.Value ??= new LearningCompletionDetectorState();

        var completed = await Mediator.Send(
            new GetCompletedLearningPacksQuery(memberId, detectorState.LastProcessedCompletedAtUtc),
            cancellationToken);

        if (completed == null || completed.Count == 0)
        {
            return;
        }

        var maxCompletedAt = detectorState.LastProcessedCompletedAtUtc;

        foreach (var row in completed)
        {
            if (string.IsNullOrWhiteSpace(row.PackId))
            {
                continue;
            }

            events.Add(new LearningCompletedEvent
            {
                MemberId = memberId,
                CourseName = row.PackId,
                CompletedBy = "learning-pack",
                CompletedAtUtc = row.CompletedAtUtc
            });

            if (row.CompletedAtUtc > maxCompletedAt)
            {
                maxCompletedAt = row.CompletedAtUtc;
            }
        }

        detectorState.LastProcessedCompletedAtUtc = maxCompletedAt;
    }
}