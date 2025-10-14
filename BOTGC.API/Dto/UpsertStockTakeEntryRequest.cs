namespace BOTGC.API.Dto
{
    public sealed record UpsertStockTakeEntryRequest(
        int StockItemId,
        string Name,
        string Division,
        string Unit,
        Guid OperatorId,
        string OperatorName,
        DateTime At,
        List<StockTakeObservationDto> Observations,
        decimal EstimatedQuantityAtCapture
    );
}
