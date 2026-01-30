using System.Text.Json;
using BOTGC.API.Common;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.EventBus.Events;
using BOTGC.API.Services.Events;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.EventBus.Subscribers;

[SubscriberName("juniorprogress-learningcompleted")]
public sealed class RecalculateJuniorProgressOnLearningCompletedSubscriber(
    ILogger<RecalculateJuniorProgressOnLearningCompletedSubscriber> logger,
    ISubscriberQueueFactory queues,
    IOptions<EventPipelineOptions> options,
    JsonSerializerOptions json,
    IJuniorProgressEvaluator evaluator)
    : SubscriberBase<LearningCompletedEvent, RecalculateJuniorProgressOnLearningCompletedSubscriber>(logger, queues, options, json)
{
    private readonly IJuniorProgressEvaluator _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));

    protected override Task ProcessAsync(LearningCompletedEvent evt, CancellationToken cancellationToken)
    {
        return _evaluator.EvaluateAsync(evt.MemberId, cancellationToken);
    }
}

[SubscriberName("juniorprogress-quizmilestonereached")]
public sealed class RecalculateJuniorProgressOnQuizMilestoneReachedSubscriber(
    ILogger<RecalculateJuniorProgressOnQuizMilestoneReachedSubscriber> logger,
    ISubscriberQueueFactory queues,
    IOptions<EventPipelineOptions> options,
    JsonSerializerOptions json,
    IJuniorProgressEvaluator evaluator)
    : SubscriberBase<QuizMilestoneReachedEvent, RecalculateJuniorProgressOnQuizMilestoneReachedSubscriber>(logger, queues, options, json)
{
    private readonly IJuniorProgressEvaluator _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));

    protected override Task ProcessAsync(QuizMilestoneReachedEvent evt, CancellationToken cancellationToken)
    {
        return _evaluator.EvaluateAsync(evt.MemberId, cancellationToken);
    }
}

[SubscriberName("juniorprogress-oncoursecheckcompleted")]
public sealed class RecalculateJuniorProgressOnOnCourseCheckCompletedSubscriber(
    ILogger<RecalculateJuniorProgressOnOnCourseCheckCompletedSubscriber> logger,
    ISubscriberQueueFactory queues,
    IOptions<EventPipelineOptions> options,
    JsonSerializerOptions json,
    IJuniorProgressEvaluator evaluator)
    : SubscriberBase<CourseAssessmentSignedOffEvent, RecalculateJuniorProgressOnOnCourseCheckCompletedSubscriber>(logger, queues, options, json)
{
    private readonly IJuniorProgressEvaluator _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));

    protected override Task ProcessAsync(CourseAssessmentSignedOffEvent evt, CancellationToken cancellationToken)
    {
        return _evaluator.EvaluateAsync(evt.MemberId, cancellationToken);
    }
}

public sealed class JuniorProgressEvaluator(
    IEnumerable<IJuniorProgressCategoryEvaluator> evaluators,
    IMemberEventWindowReader windowReader,
    IMediator mediator,
    ILogger<JuniorProgressEvaluator> logger) : IJuniorProgressEvaluator
{
    private readonly IReadOnlyList<IJuniorProgressCategoryEvaluator> _evaluators = evaluators?.ToList() ?? throw new ArgumentNullException(nameof(evaluators));
    private readonly IMemberEventWindowReader _windowReader = windowReader ?? throw new ArgumentNullException(nameof(windowReader));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILogger<JuniorProgressEvaluator> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task EvaluateAsync(int memberId, CancellationToken cancellationToken)
    {
        var w = await _windowReader.GetLatestCategoryWindowAsync(memberId, take: 3000, cancellationToken);

        var category = w.Category;

        if (string.IsNullOrWhiteSpace(category))
        {
            var member = await _mediator.Send(new GetMemberQuery
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

        var evaluator = _evaluators.FirstOrDefault(x => x.CanEvaluate(category));
        if (evaluator is null)
        {
            _logger.LogDebug("No junior progress evaluator registered for member {MemberId} (Category={Category}).", memberId, category);
            return;
        }

        await evaluator.EvaluateAsync(memberId, category, w, cancellationToken);
    }
}

public abstract class JuniorProgressCategoryEvaluatorBase(
    IEventPublisher publisher,
    JsonSerializerOptions json,
    ILogger logger)
{
    protected readonly IEventPublisher Publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
    protected readonly JsonSerializerOptions Json = json ?? throw new ArgumentNullException(nameof(json));
    protected readonly ILogger Logger = logger ?? throw new ArgumentNullException(nameof(logger));

    protected bool HasEventInWindow<TEvent>(MemberCategoryWindow window) where TEvent : IEvent
    {
        var eventTypeKey = EventTypeKey.For<TEvent>();

        foreach (var e in window.EventsNewestFirst)
        {
            if (string.Equals(e.EventClrType, eventTypeKey, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    protected bool HasQuizMilestoneForCategoryInWindow(MemberCategoryWindow window, string category)
    {
        var key = EventTypeKey.For<QuizMilestoneReachedEvent>();

        foreach (var row in window.EventsNewestFirst)
        {
            if (!string.Equals(row.EventClrType, key, StringComparison.Ordinal))
            {
                continue;
            }

            var evt = Deserialize<QuizMilestoneReachedEvent>(row.PayloadJson);
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

    protected bool HasSignedOffOnCourseCheckInWindow(MemberCategoryWindow window)
    {
        var key = EventTypeKey.For<CourseAssessmentSignedOffEvent>();

        foreach (var row in window.EventsNewestFirst)
        {
            if (!string.Equals(row.EventClrType, key, StringComparison.Ordinal))
            {
                continue;
            }

            var evt = Deserialize<CourseAssessmentSignedOffEvent>(row.PayloadJson);
            if (evt is not null)
            {
                return true;
            }
        }

        return false;
    }

    protected bool HasProgressMilestoneAlreadyInWindowForCategory(MemberCategoryWindow window, string fromCategory)
    {
        var key = EventTypeKey.For<JuniorProgressMilestoneAchievedEvent>();

        foreach (var row in window.EventsNewestFirst)
        {
            if (!string.Equals(row.EventClrType, key, StringComparison.Ordinal))
            {
                continue;
            }

            var evt = Deserialize<JuniorProgressMilestoneAchievedEvent>(row.PayloadJson);
            if (evt is null)
            {
                continue;
            }

            if (string.Equals(evt.FromCategory, fromCategory, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    protected async Task PublishMilestoneIfNotAlreadyAsync(
        int memberId,
        string fromCategory,
        string toCategory,
        string milestoneName,
        string criteriaSummary,
        MemberCategoryWindow window,
        CancellationToken cancellationToken)
    {
        if (HasProgressMilestoneAlreadyInWindowForCategory(window, fromCategory))
        {
            Logger.LogDebug("Junior progress milestone already emitted in current category window for member {MemberId} (FromCategory={FromCategory}).", memberId, fromCategory);
            return;
        }

        var evt = new JuniorProgressMilestoneAchievedEvent
        {
            MemberId = memberId,
            OccurredAtUtc = DateTimeOffset.UtcNow,
            FromCategory = fromCategory,
            ToCategory = toCategory,
            MilestoneName = milestoneName,
            CriteriaSummary = criteriaSummary,
            WindowStartUtc = window.WindowStartUtc ?? DateTimeOffset.UtcNow,
            WindowEndUtc = window.WindowEndUtc ?? DateTimeOffset.UtcNow
        };

        await Publisher.PublishAsync(evt, cancellationToken);

        Logger.LogInformation(
            "Junior progress milestone achieved for member {MemberId}. From={FromCategory} To={ToCategory}.",
            memberId,
            fromCategory,
            toCategory);
    }

    protected T? Deserialize<T>(string? payloadJson) where T : class
    {
        if (string.IsNullOrWhiteSpace(payloadJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(payloadJson, Json);
        }
        catch
        {
            return null;
        }
    }
}

public sealed class JuniorCadetProgressCategoryEvaluator(
    IEventPublisher publisher,
    JsonSerializerOptions json,
    ILogger<JuniorCadetProgressCategoryEvaluator> logger)
    : JuniorProgressCategoryEvaluatorBase(publisher, json, logger), IJuniorProgressCategoryEvaluator
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public bool CanEvaluate(string? membershipCategory)
    {
        return Comparer.Equals(membershipCategory, "Junior Cadet");
    }

    public async Task EvaluateAsync(int memberId, string category, MemberCategoryWindow window, CancellationToken cancellationToken)
    {
        var hasQuizMilestone = HasQuizMilestoneForCategoryInWindow(window, category);
        var hasSignedOffCheck = HasSignedOffOnCourseCheckInWindow(window);
        var hasParentLearning = HasEventInWindow<LearningCompletedEvent>(window);

        if (!hasQuizMilestone || !hasSignedOffCheck || !hasParentLearning)
        {
            Logger.LogInformation(
                "Junior Cadet progress not yet achieved for member {MemberId}. QuizMilestone={QuizMilestone} OnCourseSignedOff={SignedOff} ParentLearning={ParentLearning}.",
                memberId,
                hasQuizMilestone,
                hasSignedOffCheck,
                hasParentLearning);

            return;
        }

        await PublishMilestoneIfNotAlreadyAsync(
            memberId: memberId,
            fromCategory: "Junior Cadet",
            toCategory: "Junior Course Cadet",
            milestoneName: "Junior Cadet requirements met",
            criteriaSummary: "Quiz milestone reached; on-course check signed off; parent learning completed.",
            window: window,
            cancellationToken: cancellationToken);
    }
}

public sealed class JuniorCourseCadetProgressCategoryEvaluator(
    IEventPublisher publisher,
    JsonSerializerOptions json,
    ILogger<JuniorCourseCadetProgressCategoryEvaluator> logger)
    : JuniorProgressCategoryEvaluatorBase(publisher, json, logger), IJuniorProgressCategoryEvaluator
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public bool CanEvaluate(string? membershipCategory)
    {
        return Comparer.Equals(membershipCategory, "Junior Course Cadet");
    }

    public async Task EvaluateAsync(int memberId, string category, MemberCategoryWindow window, CancellationToken cancellationToken)
    {
        var hasQuizMilestone = HasQuizMilestoneForCategoryInWindow(window, category);
        var hasSignedOffCheck = HasSignedOffOnCourseCheckInWindow(window);
        var hasParentLearning = HasEventInWindow<LearningCompletedEvent>(window);

        if (!hasQuizMilestone || !hasSignedOffCheck || !hasParentLearning)
        {
            Logger.LogInformation(
                "Junior Course Cadet progress not yet achieved for member {MemberId}. QuizMilestone={QuizMilestone} OnCourseSignedOff={SignedOff} ParentLearning={ParentLearning}.",
                memberId,
                hasQuizMilestone,
                hasSignedOffCheck,
                hasParentLearning);

            return;
        }

        await PublishMilestoneIfNotAlreadyAsync(
            memberId: memberId,
            fromCategory: "Junior Course Cadet",
            toCategory: "Junior",
            milestoneName: "Junior Course Cadet requirements met",
            criteriaSummary: "Quiz milestone reached; on-course check signed off; parent learning completed.",
            window: window,
            cancellationToken: cancellationToken);
    }
}
