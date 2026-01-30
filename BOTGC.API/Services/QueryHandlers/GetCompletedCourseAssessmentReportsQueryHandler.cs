using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class GetCompletedCourseAssessmentReportsQueryHandler(
    ICourseAssessmentReportReadStore store)
    : QueryHandlerBase<GetCompletedCourseAssessmentReportsQuery, IReadOnlyList<CompletedCourseAssessmentReportDto>>
{
    private readonly ICourseAssessmentReportReadStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public override async Task<IReadOnlyList<CompletedCourseAssessmentReportDto>> Handle(
        GetCompletedCourseAssessmentReportsQuery request,
        CancellationToken cancellationToken)
    {
        var take = request.Take <= 0 ? 50 : Math.Min(request.Take, 500);

        var reports = await _store.ListReportsAsync(
            request.MemberId.ToString(),
            request.SinceUtc,
            take,
            cancellationToken);

        return reports
            .Select(x => new CompletedCourseAssessmentReportDto
            {
                ReportId = x.RowKey,
                MemberId = x.MemberId,
                RecordedBy = x.RecordedBy,
                RecordedAtUtc = x.RecordedAtUtc,
                PlayedOnDate = x.PlayedOnDate,
                EtiquetteAndSafetyOk = x.EtiquetteAndSafetyOk ?? string.Empty,
                CareForCourseOk = x.CareForCourseOk ?? string.Empty,
                PaceAndImpactOk = x.PaceAndImpactOk ?? string.Empty,
                ReadyToPlayIndependently = x.ReadyToPlayIndependently ?? string.Empty,
                RecommendedTeeBox = x.RecommendedTeeBox ?? string.Empty,
                Notes = x.Notes,
            })
            .ToArray();
    }
}