namespace BOTGC.API.Dto
{
    public sealed record StockTakeItemAcceptedDto(
        int StockItemId,
        StockTakeEntryDto StockTakeEntry,
        string Message,
        decimal? Observed,
        decimal? Estimate,
        decimal? Difference,
        decimal? Percent
    );
}
