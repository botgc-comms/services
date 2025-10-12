namespace BOTGC.API.Dto
{
    public sealed record StockTakeProductSnapshotDto(
    int StockItemId,
    string Name,
    string Unit,
    string Division,
    decimal? EstimatedQuantityAtCapture  
);
}
