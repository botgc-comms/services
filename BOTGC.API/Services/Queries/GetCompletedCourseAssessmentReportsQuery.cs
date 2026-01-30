using BOTGC.API.Models;

namespace BOTGC.API.Services.Queries;

public sealed record GetCompletedCourseAssessmentReportsQuery(
    int MemberId,
    DateTimeOffset? SinceUtc,
    int Take = 50)
    : QueryBase<IReadOnlyList<CompletedCourseAssessmentReportDto>>;