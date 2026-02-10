using System;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BOTGC.API.Common;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.EventBus.Events;
using BOTGC.API.Services.Events;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.EventBus.Detectors;

public sealed class JuniorProgressChangeDetectorState
{
    public string? LastSignature { get; set; }
}

[DetectorName("junior-progress-change-detector")]
[DetectorSchedule("0 22 * * *", runOnStartup: true)]
public sealed class JuniorProgressChangeDetector(
    IDetectorStateStore stateStore,
    IEventPublisher publisher,
    IMediator mediator,
    IDistributedLockManager lockManager,
    IJuniorCategoryProgressCalculator progressCalculator,
    IOptions<AppSettings> appSettings,
    ILogger<JuniorProgressChangeDetector> logger)
    : MemberDetectorBase<JuniorProgressChangeDetectorState>(stateStore, publisher, mediator, lockManager, appSettings, logger)
{
    private readonly IJuniorCategoryProgressCalculator _progressCalculator =
        progressCalculator ?? throw new ArgumentNullException(nameof(progressCalculator));

    protected override async Task DetectAsync(
        MemberScope scopeKey,
        DetectorState<JuniorProgressChangeDetectorState> state,
        IDetectorEventSink events,
        CancellationToken cancellationToken)
    {
        var memberId = scopeKey.MemberNumber;

        var progress = await _progressCalculator.CalculateAsync(memberId, cancellationToken);

        var newSignature = ComputeSignature(progress);

        var detectorState = state.Value ??= new JuniorProgressChangeDetectorState();
        var previousSignature = detectorState.LastSignature;

        if (string.Equals(previousSignature, newSignature, StringComparison.Ordinal))
        {
            return;
        }

        detectorState.LastSignature = newSignature;

        events.Add(new JuniorProgressChangedEvent
        {
            MemberId = progress.MemberId,
            Category = progress.Category ?? string.Empty,
            PercentComplete = progress.PercentComplete,
            Pillars = progress.Pillars,
            PreviousSignature = previousSignature,
            NewSignature = newSignature
        });
    }

    private static string ComputeSignature(JuniorCategoryProgressResult progress)
    {
        var sb = new StringBuilder(256);

        sb.Append("c=");
        sb.Append((progress.Category ?? string.Empty).Trim().ToLowerInvariant());

        sb.Append("|p=");
        sb.Append(progress.PercentComplete.ToString("0.########", CultureInfo.InvariantCulture));

        foreach (var r in progress.Pillars.OrderBy(x => x.PillarKey, StringComparer.OrdinalIgnoreCase))
        {
            sb.Append("|k=");
            sb.Append((r.PillarKey ?? string.Empty).Trim().ToLowerInvariant());

            sb.Append(",w=");
            sb.Append(r.WeightPercent.ToString("0.########", CultureInfo.InvariantCulture));

            sb.Append(",m=");
            sb.Append(r.MilestoneAchieved ? "1" : "0");

            sb.Append(",f=");
            sb.Append(r.PillarCompletionFraction.ToString("0.########", CultureInfo.InvariantCulture));

            sb.Append(",cp=");
            sb.Append(r.ContributionPercent.ToString("0.########", CultureInfo.InvariantCulture));
        }

        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(sb.ToString()));

        return Convert.ToBase64String(hash);
    }
}

[ProgressPillar("quiz", "quiz.milestone.reached")]
public sealed class QuizProgressPillar : JuniorProgressPillarBase
{
    private static readonly string _passedEventType = EventTypeKey.For(typeof(QuizPassedEvent));

    private readonly AppSettings _app;

    public QuizProgressPillar(IOptions<AppSettings> appSettings)
    {
        _app = appSettings?.Value ?? throw new ArgumentNullException(nameof(appSettings));
    }

    protected override Task<double> EvaluatePartialCompletionFractionAsync(
        int memberId,
        string category,
        IReadOnlyList<EventStreamEntity> categoryEventsNewestFirst,
        JuniorPillarRule pillarRule,
        CancellationToken cancellationToken)
    {
        var required = ResolveRequiredPassedCount(category);
        if (required <= 0)
        {
            return Task.FromResult(0.0d);
        }

        var achieved = categoryEventsNewestFirst.Count(e =>
            EventTypeMatcher.IsMatch(e.EventClrType, _passedEventType));

        if (achieved <= 0)
        {
            return Task.FromResult(0.0d);
        }

        var fraction = (double)achieved / required;
        return Task.FromResult(fraction >= 1.0d ? 1.0d : fraction);
    }

    private int ResolveRequiredPassedCount(string category)
    {
        var rules = _app.Events?.QuizMilestones?.Rules;
        if (rules is null || rules.Count == 0)
        {
            return 0;
        }

        var rule = rules.FirstOrDefault(r =>
            string.Equals(r.MembershipCategory, category, StringComparison.OrdinalIgnoreCase));

        return rule?.RequiredPassedCount ?? 0;
    }
}

[ProgressPillar("parentsLearningPacks", "learning.milestone.parent.completed")]
public sealed class ParentsLearningPacksProgressPillar : JuniorProgressPillarBase
{
    private readonly IMediator _mediator;
    private readonly JsonSerializerOptions _json;

    public ParentsLearningPacksProgressPillar(
        IMediator mediator,
        JsonSerializerOptions json)
    {
        _mediator = mediator
            ?? throw new ArgumentNullException(nameof(mediator));

        _json = json
            ?? throw new ArgumentNullException(nameof(json));
    }

    protected override async Task<double> EvaluatePartialCompletionFractionAsync(
        int memberId,
        string category,
        IReadOnlyList<EventStreamEntity> categoryEventsNewestFirst,
        JuniorPillarRule pillarRule,
        CancellationToken cancellationToken)
    {
        var mandatory = await _mediator.Send(
            new GetMandatoryLearningPacksForChildQuery(memberId),
            cancellationToken);

        if (mandatory is null || mandatory.MandatoryPackIds.Count == 0)
        {
            return 0.0d;
        }

        var required = mandatory.MandatoryPackIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (required.Count == 0)
        {
            return 0.0d;
        }

        var completedEventType =
            EventTypeKey.For(typeof(ParentLearningCompletedEvent));

        var completedDistinct =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var e in categoryEventsNewestFirst)
        {
            if (!EventTypeMatcher.IsMatch(e.EventClrType, completedEventType))
            {
                continue;
            }

            var evt = Deserialize<ParentLearningCompletedEvent>(e.PayloadJson);
            if (evt is null || string.IsNullOrWhiteSpace(evt.CourseName))
            {
                continue;
            }

            var courseName = evt.CourseName.Trim();

            if (required.Contains(courseName))
            {
                completedDistinct.Add(courseName);
            }
        }

        if (completedDistinct.Count == 0)
        {
            return 0.0d;
        }

        var fraction = (double)completedDistinct.Count / required.Count;
        return fraction >= 1.0d ? 1.0d : fraction;
    }

    private T? Deserialize<T>(string payloadJson) where T : class
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(payloadJson, _json);
        }
        catch
        {
            return null;
        }
    }
}

[ProgressPillar("onCourseAssessment", "course-assessment.signed-off")]
public sealed class OnCourseAssessmentProgressPillar : JuniorProgressPillarBase
{
    private static readonly string _filedEventType = EventTypeKey.For(typeof(CourseAssessmentReportFiledEvent));
    private static readonly string _signedOffEventType = EventTypeKey.For(typeof(CourseAssessmentSignedOffEvent));

    protected override Task<double> EvaluatePartialCompletionFractionAsync(
        int memberId,
        string category,
        IReadOnlyList<EventStreamEntity> categoryEventsNewestFirst,
        JuniorPillarRule pillarRule,
        CancellationToken cancellationToken)
    {
        var hasFiledReport = categoryEventsNewestFirst.Any(e =>
            EventTypeMatcher.IsMatch(e.EventClrType, _filedEventType));

        return Task.FromResult(hasFiledReport ? 0.5d : 0.0d);
    }
}


