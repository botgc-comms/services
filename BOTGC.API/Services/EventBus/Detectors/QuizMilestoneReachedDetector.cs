using System.Text.Json;
using BOTGC.API.Common;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.EventBus;
using BOTGC.API.Services.EventBus.Events;
using BOTGC.API.Services.Events;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.Events.Detectors;

public sealed class QuizMilestoneDetectorState
{
    public DateTimeOffset? WindowStartUtc { get; set; }
    public DateTimeOffset? MilestoneRaisedAtUtc { get; set; }
}

[DetectorSchedule("0 22 * * *", runOnStartup: true)]
public sealed class QuizMilestoneReachedDetector(
    IDetectorStateStore stateStore,
    IEventPublisher publisher,
    IMediator mediator,
    IDistributedLockManager lockManager,
    IMemberEventWindowReader windowReader,
    IOptions<AppSettings> appSettings,
    JsonSerializerOptions json,
    ILogger<QuizMilestoneReachedDetector> logger)
    : MemberDetectorBase<QuizMilestoneDetectorState>(stateStore, publisher, mediator, lockManager, appSettings, logger)
{
    private readonly IMemberEventWindowReader _windowReader = windowReader ?? throw new ArgumentNullException(nameof(windowReader));
    private readonly AppSettings _settings = appSettings?.Value ?? throw new ArgumentNullException(nameof(appSettings));
    private readonly JsonSerializerOptions _json = json ?? throw new ArgumentNullException(nameof(json));

    public override string Name => "quiz-milestone-reached-detector";

    protected override async Task DetectAsync(
        MemberScope scopeKey,
        DetectorState<QuizMilestoneDetectorState> state,
        IDetectorEventSink events,
        CancellationToken cancellationToken)
    {
        var memberId = scopeKey.MemberNumber;

        var take = 3000;

        var w = await _windowReader.GetLatestCategoryWindowAsync(memberId, take, cancellationToken);

        var window = w.EventsNewestFirst;

        if (window.Count == 0)
        {
            return;
        }

        var windowIsReliable = window.Count < take && w.WindowStartUtc.HasValue;

        if (windowIsReliable)
        {
            if (state.Value.WindowStartUtc.HasValue && w.WindowStartUtc!.Value > state.Value.WindowStartUtc.Value)
            {
                state.Value.WindowStartUtc = w.WindowStartUtc;
                state.Value.MilestoneRaisedAtUtc = null;
            }
            else if (!state.Value.WindowStartUtc.HasValue)
            {
                state.Value.WindowStartUtc = w.WindowStartUtc;
            }

            if (state.Value.MilestoneRaisedAtUtc.HasValue &&
                state.Value.WindowStartUtc.HasValue &&
                state.Value.MilestoneRaisedAtUtc.Value >= state.Value.WindowStartUtc.Value)
            {
                return;
            }
        }

        var sawPassedQuizInWindow = false;
        var milestoneAlreadyInWindowForCategory = false;

        var categoryFromEvents = w.Category;

        foreach (var e in window)
        {
            if (IsEventType<QuizPassedEvent>(e.EventClrType))
            {
                var passedEvt = Deserialize<QuizPassedEvent>(e);
                if (passedEvt is not null && passedEvt.CorrectCount >= passedEvt.PassMark)
                {
                    sawPassedQuizInWindow = true;
                }

                continue;
            }

            if (IsEventType<QuizMilestoneReachedEvent>(e.EventClrType))
            {
                var milestoneEvt = Deserialize<QuizMilestoneReachedEvent>(e);
                if (milestoneEvt is not null && !string.IsNullOrWhiteSpace(milestoneEvt.Category))
                {
                    if (!string.IsNullOrWhiteSpace(categoryFromEvents) &&
                        string.Equals(milestoneEvt.Category, categoryFromEvents, StringComparison.OrdinalIgnoreCase))
                    {
                        milestoneAlreadyInWindowForCategory = true;
                    }
                }
            }
        }

        if (!sawPassedQuizInWindow)
        {
            return;
        }

        var category = categoryFromEvents;

        if (string.IsNullOrWhiteSpace(category))
        {
            var member = await Mediator.Send(new GetMemberQuery
            {
                MemberNumber = memberId,
                Cache = new CacheOptions(NoCache: true, WriteThrough: true)
            }, cancellationToken);

            category = member?.MembershipCategory;
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            return;
        }

        var rule = ResolveRule(category);
        if (rule is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(categoryFromEvents) && milestoneAlreadyInWindowForCategory)
        {
            return;
        }

        if (HasMilestoneAlreadyInWindow(window, category))
        {
            return;
        }

        var passedCount = CountPassedInWindow(window, rule.MinimumDifficulty);

        if (passedCount < rule.RequiredPassedCount)
        {
            return;
        }

        var occurredAtUtc = DateTimeOffset.UtcNow;

        events.Add(new QuizMilestoneReachedEvent
        {
            MemberId = memberId,
            Category = category,
            RequiredPassedCount = rule.RequiredPassedCount,
            MinimumDifficulty = rule.MinimumDifficulty,
            PassedCountInWindow = passedCount,
            WindowStartUtc = w.WindowStartUtc ?? occurredAtUtc,
            WindowEndUtc = w.WindowEndUtc ?? occurredAtUtc,
            OccurredAtUtc = occurredAtUtc,
            MilestoneAchieved = rule.Name
        });

        state.Value.MilestoneRaisedAtUtc = occurredAtUtc;

        if (windowIsReliable && w.WindowStartUtc.HasValue && !state.Value.WindowStartUtc.HasValue)
        {
            state.Value.WindowStartUtc = w.WindowStartUtc;
        }
    }


    private QuizMilestoneRule? ResolveRule(string category)
    {
        var rules = _settings.Events.QuizMilestones.Rules;
        return rules.FirstOrDefault(r => string.Equals(r.MembershipCategory, category, StringComparison.OrdinalIgnoreCase));
    }

    private bool HasMilestoneAlreadyInWindow(IReadOnlyList<EventStreamEntity> windowDesc, string category)
    {
        foreach (var e in windowDesc)
        {
            if (!IsEventType<QuizMilestoneReachedEvent>(e.EventClrType))
            {
                continue;
            }

            var evt = Deserialize<QuizMilestoneReachedEvent>(e);
            if (evt is null)
            {
                continue;
            }

            if (string.Equals(evt.Category, category, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private int CountPassedInWindow(IReadOnlyList<EventStreamEntity> windowDesc, QuizDifficulty minimumDifficulty)
    {
        var count = 0;

        foreach (var e in windowDesc)
        {
            if (!IsEventType<QuizPassedEvent>(e.EventClrType))
            {
                continue;
            }

            var evt = Deserialize<QuizPassedEvent>(e);
            if (evt is null)
            {
                continue;
            }

            if (evt.CorrectCount < evt.PassMark)
            {
                continue;
            }

            if (evt.Difficulty < minimumDifficulty)
            {
                continue;
            }

            count++;
        }

        return count;
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
