namespace BOTGC.API.Dto
{
    public sealed record StockTakeEntryDto(
    int StockItemId,
    string Name,
    string Division,
    string Unit,
    Guid OperatorId,
    string OperatorName,
    DateTimeOffset At,
    List<StockTakeObservationDto> Observations
);
}
