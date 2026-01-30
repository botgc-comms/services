using BOTGC.API.Interfaces;
using BOTGC.API.Models;

public sealed class TableStorageCourseAssessmentReportReadStore(
    ITableStore<CourseAssessmentReportEntity> reportsTable)
    : ICourseAssessmentReportReadStore
{
    private readonly ITableStore<CourseAssessmentReportEntity> _reportsTable = reportsTable ?? throw new ArgumentNullException(nameof(reportsTable));

    public async Task<IReadOnlyList<CourseAssessmentReportEntity>> ListReportsAsync(
        string memberId,
        DateTimeOffset? sinceUtc,
        int take,
        CancellationToken cancellationToken)
    {
        var raw = await _reportsTable.QueryAsync(
            predicate: e => e.PartitionKey == memberId,
            take: 1000,
            cancellationToken: cancellationToken);

        IEnumerable<CourseAssessmentReportEntity> filtered = raw;

        if (sinceUtc.HasValue)
        {
            var cut = sinceUtc.Value;
            filtered = filtered.Where(x => x.RecordedAtUtc > cut);
        }

        return filtered
            .OrderByDescending(x => x.RecordedAtUtc)
            .Take(take <= 0 ? 50 : take)
            .ToArray();
    }
}