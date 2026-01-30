namespace BOTGC.API.Models;

public sealed class CompletedCourseAssessmentReportDto
{
    public required string ReportId { get; init; }

    public required int MemberId { get; init; }

    public required string RecordedBy { get; init; }

    public required DateTimeOffset RecordedAtUtc { get; init; }

    public required DateTime PlayedOnDate { get; init; }

    public required string EtiquetteAndSafetyOk { get; init; }

    public required string CareForCourseOk { get; init; }

    public required string PaceAndImpactOk { get; init; }

    public required string ReadyToPlayIndependently { get; init; }

    public required string RecommendedTeeBox { get; init; }

    public string? Notes { get; init; }
}