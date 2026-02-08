using System.Text.Json;
using BOTGC.API.Common;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.EventBus;
using BOTGC.API.Services.EventBus.Events;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.Events.Detectors;

public sealed class ParentLearningProjectionDetectorState
{
    public Dictionary<int, DateTimeOffset> LastProcessedCompletedAtUtcByParent { get; set; } = new();
}

[DetectorSchedule("0 20 * * *", runOnStartup: true)]
public sealed class ParentLearningProjectionDetector(
    IDetectorStateStore stateStore,
    IEventPublisher publisher,
    IMediator mediator,
    IDistributedLockManager lockManager,
    IMemberEventWindowReader windowReader,
    IOptions<AppSettings> appSettings,
    JsonSerializerOptions json,
    ILogger<ParentLearningProjectionDetector> logger)
    : MemberDetectorBase<ParentLearningProjectionDetectorState>(stateStore, publisher, mediator, lockManager, appSettings, logger)
{
    private readonly IMemberEventWindowReader _windowReader = windowReader ?? throw new ArgumentNullException(nameof(windowReader));
    private readonly AppSettings _app = appSettings?.Value ?? throw new ArgumentNullException(nameof(appSettings));
    private readonly JsonSerializerOptions _json = json ?? throw new ArgumentNullException(nameof(json));

    public override string Name => "parent-learning-projection-detector";

    protected override async Task DetectAsync(
        MemberScope scopeKey,
        DetectorState<ParentLearningProjectionDetectorState> state,
        IDetectorEventSink events,
        CancellationToken cancellationToken)
    {
        var childMemberId = scopeKey.MemberNumber;

        state.Value ??= new ParentLearningProjectionDetectorState();

        var parents = await Mediator.Send(new GetParentsQuery(), cancellationToken);

        var parentIds = parents
            .Where(p => p.Children != null && p.Children.Contains(childMemberId))
            .Select(p => p.ParentMemberId)
            .Distinct()
            .ToArray();

        if (parentIds.Length == 0)
        {
            return;
        }

        var childTake = _app.Events?.JuniorProgress?.CategoryWindowTake ?? 500;
        var childWindow = await _windowReader.GetLatestCategoryWindowAsync(childMemberId, childTake, cancellationToken);

        var alreadyRaisedForCourse = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var e in childWindow.EventsNewestFirst)
        {
            if (!IsEventType<ParentLearningCompletedEvent>(e.EventClrType))
            {
                continue;
            }

            var existing = Deserialize<ParentLearningCompletedEvent>(e);
            if (existing is null || string.IsNullOrWhiteSpace(existing.CourseName))
            {
                continue;
            }

            alreadyRaisedForCourse.Add(existing.CourseName.Trim());
        }

        var newlyObservedAcrossParents = new Dictionary<string, (DateTimeOffset completedAtUtc, int parentId)>(StringComparer.OrdinalIgnoreCase);

        foreach (var parentId in parentIds)
        {
            if (!state.Value.LastProcessedCompletedAtUtcByParent.TryGetValue(parentId, out var lastProcessed))
            {
                lastProcessed = DateTimeOffset.MinValue;
            }

            var take = Math.Max(childTake, 2000);
            var parentWindow = await _windowReader.GetLatestCategoryWindowAsync(parentId, take, cancellationToken);

            var maxCompletedAt = lastProcessed;

            foreach (var e in parentWindow.EventsNewestFirst)
            {
                if (!IsEventType<LearningCompletedEvent>(e.EventClrType))
                {
                    continue;
                }

                var completedEvt = Deserialize<LearningCompletedEvent>(e);
                if (completedEvt is null || string.IsNullOrWhiteSpace(completedEvt.CourseName))
                {
                    continue;
                }

                var completedAtUtc = completedEvt.CompletedAtUtc;
                if (completedAtUtc <= lastProcessed)
                {
                    continue;
                }

                if (completedAtUtc > maxCompletedAt)
                {
                    maxCompletedAt = completedAtUtc;
                }

                var courseName = completedEvt.CourseName.Trim();

                if (alreadyRaisedForCourse.Contains(courseName))
                {
                    continue;
                }

                if (newlyObservedAcrossParents.TryGetValue(courseName, out var existing))
                {
                    if (completedAtUtc < existing.completedAtUtc)
                    {
                        newlyObservedAcrossParents[courseName] = (completedAtUtc, parentId);
                    }
                }
                else
                {
                    newlyObservedAcrossParents[courseName] = (completedAtUtc, parentId);
                }
            }

            state.Value.LastProcessedCompletedAtUtcByParent[parentId] = maxCompletedAt;
        }

        if (newlyObservedAcrossParents.Count == 0)
        {
            return;
        }

        var occurredAtUtc = DateTimeOffset.UtcNow;

        foreach (var kvp in newlyObservedAcrossParents.OrderBy(x => x.Value.completedAtUtc))
        {
            var courseName = kvp.Key;
            var (completedAtUtc, parentId) = kvp.Value;

            events.Add(new ParentLearningCompletedEvent
            {
                MemberId = childMemberId,
                ParentMemberId = parentId,
                CourseName = courseName,
                CompletedBy = "parent",
                CompletedAtUtc = completedAtUtc,
                OccurredAtUtc = occurredAtUtc
            });
        }
    }

    private static bool IsEventType<T>(string eventClrType) where T : IEvent
    {
        var key = EventTypeKey.For(typeof(T));
        return string.Equals(eventClrType, key, StringComparison.Ordinal);
    }

    private T? Deserialize<T>(EventStreamEntity entity) where T : class
    {
        if (string.IsNullOrWhiteSpace(entity.PayloadJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(entity.PayloadJson, _json);
        }
        catch
        {
            return null;
        }
    }
}
