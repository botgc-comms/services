using Azure.Data.Tables;
using Azure;

namespace BOTGC.API.Models;

public sealed class CourseAssessmentReportEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;

    public string RowKey { get; set; } = string.Empty;

    public DateTimeOffset? Timestamp { get; set; }

    public ETag ETag { get; set; }

    public int MemberId { get; set; }

    public string RecordedBy { get; set; } = string.Empty;

    public DateTimeOffset RecordedAtUtc { get; set; }

    public DateTime PlayedOnDate { get; set; }

    public string? EtiquetteAndSafetyOk { get; set; }

    public string? CareForCourseOk { get; set; }

    public string? PaceAndImpactOk { get; set; }

    public string? ReadyToPlayIndependently { get; set; }

    public string? RecommendedTeeBox { get; set; }

    public string? Notes { get; set; }
}