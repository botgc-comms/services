using BOTGC.API.Common;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.EventBus;
using BOTGC.API.Services.EventBus.Events;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.Events.Detectors;

public sealed class CourseAssessmentDetectorState
{
    public DateTimeOffset LastProcessedRecordedAtUtc { get; set; } = DateTimeOffset.MinValue;
}

[DetectorName("course-assessment-completion-detector")]
[DetectorSchedule("0 22 * * *", runOnStartup: true)]
public sealed class CourseAssessmentCompletionDetector(
    IDetectorStateStore stateStore,
    IEventPublisher publisher,
    IMediator mediator,
    IDistributedLockManager lockManager,
    IOptions<AppSettings> appSettings,
    ILogger<CourseAssessmentCompletionDetector> logger)
    : MemberDetectorBase<CourseAssessmentDetectorState>(stateStore, publisher, mediator, lockManager, appSettings, logger)
{
    protected override async Task DetectAsync(
        MemberScope scopeKey,
        DetectorState<CourseAssessmentDetectorState> state,
        IDetectorEventSink events,
        CancellationToken cancellationToken)
    {
        var memberId = scopeKey.MemberNumber;

        var detectorState = state.Value ??= new CourseAssessmentDetectorState();

        var reports = await Mediator.Send(
            new GetCompletedCourseAssessmentReportsQuery(memberId, detectorState.LastProcessedRecordedAtUtc),
            cancellationToken);

        if (reports.Count == 0)
        {
            return;
        }

        var maxRecordedAt = detectorState.LastProcessedRecordedAtUtc;

        foreach (var report in reports)
        {
            var reportId = report.ReportId;

            events.Add(new CourseAssessmentReportFiledEvent
            {
                MemberId = memberId,
                ReportId = reportId,
                RecordedBy = report.RecordedBy,
                RecordedAtUtc = report.RecordedAtUtc,
                PlayedOnDate = report.PlayedOnDate,
                EtiquetteAndSafetyOk = report.EtiquetteAndSafetyOk,
                CareForCourseOk = report.CareForCourseOk,
                PaceAndImpactOk = report.PaceAndImpactOk,
                ReadyToPlayIndependently = report.ReadyToPlayIndependently,
                RecommendedTeeBox = report.RecommendedTeeBox,
                Notes = report.Notes,
            });

            if (ParseAnswer(report.ReadyToPlayIndependently) == CourseAssessmentAnswer.Yes)
            {
                events.Add(new CourseAssessmentSignedOffEvent
                {
                    MemberId = memberId,
                    ReportId = reportId,
                    RecordedBy = report.RecordedBy,
                    RecordedAtUtc = report.RecordedAtUtc,
                    PlayedOnDate = report.PlayedOnDate,
                    RecommendedTeeBox = report.RecommendedTeeBox,
                    Notes = report.Notes,
                });
            }

            if (report.RecordedAtUtc > maxRecordedAt)
            {
                maxRecordedAt = report.RecordedAtUtc;
            }
        }

        detectorState.LastProcessedRecordedAtUtc = maxRecordedAt;
    }

    private static CourseAssessmentAnswer ParseAnswer(string raw)
    {
        if (Enum.TryParse<CourseAssessmentAnswer>(raw, ignoreCase: true, out var parsed))
        {
            return parsed;
        }

        return CourseAssessmentAnswer.Unknown;
    }
}
