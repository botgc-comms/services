namespace BOTGC.MemberPortal.Models;

public sealed class CheckRideReportRecord
{
    public required int MemberId { get; init; }

    public required string RecordedBy { get; init; }

    public required DateTimeOffset RecordedAtUtc { get; init; }

    public required DateTime PlayedOn { get; init; }

    public required CheckRideAssessmentAnswer EtiquetteAndSafetyOk { get; init; }

    public required CheckRideAssessmentAnswer CareForCourseOk { get; init; }

    public required CheckRideAssessmentAnswer PaceAndImpactOk { get; init; }

    public required CheckRideAssessmentAnswer ReadyToPlayIndependently { get; init; }

    public required string RecommendedTeeBox { get; init; }

    public string? Notes { get; init; }
}
