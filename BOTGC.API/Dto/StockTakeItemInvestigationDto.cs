namespace BOTGC.API.Dto
{
    public sealed record StockTakeItemInvestigationDto(
        int StockItemId,
        StockTakeEntryDto StockTakeEntry,
        string Message,
        decimal? Observed,
        decimal? Estimate,
        decimal? Difference,
        decimal? Percent,
        string Reason,
        decimal? Allowed,
        string? DominantRule
    );
}
