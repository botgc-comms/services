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
        public decimal EntryFee { get; set; }
        public int Divisions { get; set; }
        public decimal PayoutPercent { get; set; }
        public decimal Revenue { get; set; }
        public decimal PrizePot { get; set; }
        public decimal CharityAmount { get; set; }
        public decimal ClubIncome { get; set; }
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
                EntryFee = r.EntryFee,
                Divisions = r.Divisions,
                PayoutPercent = r.PayoutPercent,
                Revenue = r.Revenue,
                PrizePot = r.PrizePot,
                CharityAmount = r.CharityAmount,
                ClubIncome = r.ClubIncome,
                RuleSet = r.RuleSet,
                Currency = r.Currency
            };
        }
    }
}