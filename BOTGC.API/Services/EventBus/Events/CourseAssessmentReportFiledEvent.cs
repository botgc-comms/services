using BOTGC.API.Interfaces;

namespace BOTGC.API.Services.EventBus.Events;

[EventType("course-assessment.report-filed", 1)]
public sealed class CourseAssessmentReportFiledEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public int MemberId { get; init; }

    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public required string ReportId { get; init; }

    public required string RecordedBy { get; init; }

    public DateTimeOffset RecordedAtUtc { get; init; }

    public DateTime PlayedOnDate { get; init; }

    public required string EtiquetteAndSafetyOk { get; init; }

    public required string CareForCourseOk { get; init; }

    public required string PaceAndImpactOk { get; init; }

    public required string ReadyToPlayIndependently { get; init; }

    public required string RecommendedTeeBox { get; init; }

    public string? Notes { get; init; }
}
