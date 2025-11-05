using Azure.Data.Tables;
using Azure;
using BOTGC.API.Dto;

namespace BOTGC.API.Models;

public sealed partial class BottleCalibrationEntity
{
    public sealed class CompetitionPayoutHeaderEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public int CompetitionId { get; set; }
        public string CompetitionName { get; set; } = string.Empty;
        public DateTime CompetitionDate { get; set; }
        public int Entrants { get; set; }
        public double EntryFee { get; set; }
        public int Divisions { get; set; }
        public double PayoutPercent { get; set; }
        public double Revenue { get; set; }
        public double PrizePot { get; set; }
        public double CharityAmount { get; set; }
        public double ClubIncome { get; set; }
        public string RuleSet { get; set; } = string.Empty;
        public string Currency { get; set; } = "GBP";

        public static CompetitionPayoutHeaderEntity FromResult(CompetitionPayoutResultDto r)
        {
            var dateUtc = r.CompetitionDate.Kind == DateTimeKind.Utc
                ? r.CompetitionDate.Date
                : DateTime.SpecifyKind(r.CompetitionDate.Date, DateTimeKind.Utc);

            return new CompetitionPayoutHeaderEntity
            {
                PartitionKey = $"{dateUtc:yyyy-MM}",
                RowKey = r.CompetitionId.ToString(),
                CompetitionId = r.CompetitionId,
                CompetitionName = r.CompetitionName,
                CompetitionDate = dateUtc,
                Entrants = r.Entrants,
                EntryFee = (double)r.EntryFee,
                Divisions = r.Divisions,
                PayoutPercent = (double)r.PayoutPercent,
                Revenue = (double)r.Revenue,
                PrizePot = (double)r.PrizePot,
                CharityAmount = (double)r.CharityAmount,
                ClubIncome = (double)r.ClubIncome,
                RuleSet = r.RuleSet,
                Currency = r.Currency
            };
        }
    }
}
