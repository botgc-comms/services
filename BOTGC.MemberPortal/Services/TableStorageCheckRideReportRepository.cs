using Azure;
using Azure.Data.Tables;
using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Services;

public sealed class TableStorageCheckRideReportRepository : ICheckRideReportRepository
{
    private readonly TableClient _table;

    public TableStorageCheckRideReportRepository(AppSettings settings)
    {
        var ts = settings.Admin?.TableStorage ?? throw new InvalidOperationException("AppSettings.Admin.TableStorage is not configured.");

        var service = new TableServiceClient(ts.ConnectionString);
        _table = service.GetTableClient(ts.CheckRideReportsTableName);

        _table.CreateIfNotExists();
    }

    public Task CreateAsync(CheckRideReportRecord record, CancellationToken cancellationToken = default)
    {
        var entity = CheckRideReportEntity.FromModel(record);
        return _table.AddEntityAsync(entity, cancellationToken);
    }

    public async Task<CheckRideReportRecord?> GetSignedOffReportAsync(int memberId, CancellationToken cancellationToken = default)
    {
        var pk = memberId.ToString();

        var filter =
            TableClient.CreateQueryFilter($"PartitionKey eq {pk}") +
            " and " +
            TableClient.CreateQueryFilter($"ReadyToPlayIndependently eq {"Yes"}");

        await foreach (var e in _table.QueryAsync<CheckRideReportEntity>(filter: filter, maxPerPage: 10, cancellationToken: cancellationToken))
        {
            return e.ToModel();
        }

        return null;
    }

    private sealed class CheckRideReportEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public int MemberId { get; set; }

        public string RecordedBy { get; set; } = string.Empty;

        public DateTimeOffset RecordedAtUtc { get; set; }

        public DateTime PlayedOnDate { get; set; }

        public string EtiquetteAndSafetyOk { get; set; } = string.Empty;

        public string CareForCourseOk { get; set; } = string.Empty;

        public string PaceAndImpactOk { get; set; } = string.Empty;

        public string ReadyToPlayIndependently { get; set; } = string.Empty;

        public string RecommendedTeeBox { get; set; } = string.Empty;

        public string? Notes { get; set; }

        public static CheckRideReportEntity FromModel(CheckRideReportRecord m)
        {
            var pk = $"{m.MemberId}";
            var rk = $"CHECKRIDE_{m.RecordedAtUtc:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}";

            return new CheckRideReportEntity
            {
                PartitionKey = pk,
                RowKey = rk,
                MemberId = m.MemberId,
                RecordedBy = m.RecordedBy,
                RecordedAtUtc = m.RecordedAtUtc,
                PlayedOnDate = m.PlayedOn,
                EtiquetteAndSafetyOk = m.EtiquetteAndSafetyOk.ToString(),
                CareForCourseOk = m.CareForCourseOk.ToString(),
                PaceAndImpactOk = m.PaceAndImpactOk.ToString(),
                ReadyToPlayIndependently = m.ReadyToPlayIndependently.ToString(),
                RecommendedTeeBox = m.RecommendedTeeBox,
                Notes = m.Notes,
            };
        }

        public CheckRideReportRecord ToModel()
        {
            return new CheckRideReportRecord
            {
                MemberId = MemberId,
                RecordedBy = RecordedBy,
                RecordedAtUtc = RecordedAtUtc,
                PlayedOn = PlayedOnDate,
                EtiquetteAndSafetyOk = Enum.Parse<CheckRideAssessmentAnswer>(EtiquetteAndSafetyOk, true),
                CareForCourseOk = Enum.Parse<CheckRideAssessmentAnswer>(CareForCourseOk, true),
                PaceAndImpactOk = Enum.Parse<CheckRideAssessmentAnswer>(PaceAndImpactOk, true),
                ReadyToPlayIndependently = Enum.Parse<CheckRideAssessmentAnswer>(ReadyToPlayIndependently, true),
                RecommendedTeeBox = RecommendedTeeBox,
                Notes = Notes,
            };
        }
    }
}
