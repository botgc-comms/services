namespace BOTGC.API.Dto
{
    public sealed record StockTakeReportItemDto(
        int StockItemId,
        string Name,
        string Division,
        string Unit,
        decimal EstimatedQuantity,
        decimal ObservedQuantity,
        decimal DeviationPercent
    );
}
