using System.Text.Json;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.EventBus.Detectors;
using BOTGC.API.Services.EventBus.Events;
using BOTGC.API.Services.Events;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.EventBus.Subscribers;

[SubscriberName("junior-progress-trigger-quiz")]
public sealed class TriggerJuniorProgressOnQuizPassedSubscriber(
    ILogger<TriggerJuniorProgressOnQuizPassedSubscriber> logger,
    ISubscriberQueueFactory queues,
    IOptions<EventPipelineOptions> options,
    JsonSerializerOptions json,
    IMemberDetectorTriggerScheduler triggerScheduler)
    : SubscriberBase<QuizPassedEvent, TriggerJuniorProgressOnQuizPassedSubscriber>(logger, queues, options, json)
{
    private readonly IMemberDetectorTriggerScheduler _triggerScheduler = triggerScheduler ?? throw new ArgumentNullException(nameof(triggerScheduler));

    private static readonly string Detector = DetectorName.For(typeof(JuniorProgressChangeDetector));
    private static readonly TimeSpan QuietPeriod = TimeSpan.FromSeconds(5);

    protected override Task ProcessAsync(QuizPassedEvent evt, CancellationToken cancellationToken)
    {
        _triggerScheduler.Schedule(
            detectorName: Detector,
            memberId: evt.MemberId,
            correlationId: evt.EventId.ToString("N"),
            quietPeriod: QuietPeriod,
            stoppingToken: cancellationToken);

        return Task.CompletedTask;
    }
}

[SubscriberName("junior-progress-trigger-assessment")]
public sealed class TriggerJuniorProgressOnCourseAssessmentSignedOffSubscriber(
    ILogger<TriggerJuniorProgressOnCourseAssessmentSignedOffSubscriber> logger,
    ISubscriberQueueFactory queues,
    IOptions<EventPipelineOptions> options,
    JsonSerializerOptions json,
    IMemberDetectorTriggerScheduler triggerScheduler)
    : SubscriberBase<CourseAssessmentSignedOffEvent, TriggerJuniorProgressOnCourseAssessmentSignedOffSubscriber>(logger, queues, options, json)
{
    private readonly IMemberDetectorTriggerScheduler _triggerScheduler = triggerScheduler ?? throw new ArgumentNullException(nameof(triggerScheduler));

    private static readonly string Detector = DetectorName.For(typeof(JuniorProgressChangeDetector));
    private static readonly TimeSpan QuietPeriod = TimeSpan.FromSeconds(5);

    protected override Task ProcessAsync(CourseAssessmentSignedOffEvent evt, CancellationToken cancellationToken)
    {
        _triggerScheduler.Schedule(
            detectorName: Detector,
            memberId: evt.MemberId,
            correlationId: evt.EventId.ToString("N"),
            quietPeriod: QuietPeriod,
            stoppingToken: cancellationToken);

        return Task.CompletedTask;
    }
}

[SubscriberName("junior-progress-trigger-learning")]
public sealed class TriggerJuniorProgressOnParentLearningCompletedSubscriber(
    ILogger<TriggerJuniorProgressOnParentLearningCompletedSubscriber> logger,
    ISubscriberQueueFactory queues,
    IOptions<EventPipelineOptions> options,
    JsonSerializerOptions json,
    IMemberDetectorTriggerScheduler triggerScheduler)
    : SubscriberBase<ParentLearningCompletedEvent, TriggerJuniorProgressOnParentLearningCompletedSubscriber>(logger, queues, options, json)
{
    private readonly IMemberDetectorTriggerScheduler _triggerScheduler = triggerScheduler ?? throw new ArgumentNullException(nameof(triggerScheduler));

    private static readonly string Detector = DetectorName.For(typeof(JuniorProgressChangeDetector));
    private static readonly TimeSpan QuietPeriod = TimeSpan.FromSeconds(5);

    protected override Task ProcessAsync(ParentLearningCompletedEvent evt, CancellationToken cancellationToken)
    {
        _triggerScheduler.Schedule(
            detectorName: Detector,
            memberId: evt.MemberId,
            correlationId: evt.EventId.ToString("N"),
            quietPeriod: QuietPeriod,
            stoppingToken: cancellationToken);

        return Task.CompletedTask;
    }
}
