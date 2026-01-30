using BOTGC.API.Models;

namespace BOTGC.API.Interfaces;

public interface ICourseAssessmentReportReadStore
{
    Task<IReadOnlyList<CourseAssessmentReportEntity>> ListReportsAsync(
        string memberId,
        DateTimeOffset? sinceUtc,
        int take,
        CancellationToken cancellationToken);
}