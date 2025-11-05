using Azure.Data.Tables;
using Azure;
using BOTGC.API.Dto;

namespace BOTGC.API.Models;

public sealed class CompetitionPayoutWinnerEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty;
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public int CompetitionId { get; set; }
    public int DivisionNumber { get; set; }
    public string DivisionName { get; set; } = string.Empty;
    public int Position { get; set; }
    public string CompetitorId { get; set; } = string.Empty;
    public string CompetitorName { get; set; } = string.Empty;
    public double Amount { get; set; }
    public string Currency { get; set; } = "GBP";

    public static CompetitionPayoutWinnerEntity FromWinner(WinnerPayoutDto w)
    {
        return new CompetitionPayoutWinnerEntity
        {
            PartitionKey = w.CompetitionId.ToString(),
            RowKey = $"{w.DivisionNumber:D2}-{w.Position:D2}-{w.CompetitorId}",
            CompetitionId = w.CompetitionId,
            DivisionNumber = w.DivisionNumber,
            DivisionName = w.DivisionName,
            Position = w.Position,
            CompetitorId = w.CompetitorId,
            CompetitorName = w.CompetitorName,
            Amount = (double)w.Amount,
            Currency = w.Currency
        };
    }
}
