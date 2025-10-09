using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public sealed record UpsertStockTakeEntryCommand(
        DateTime Date,
        int StockItemId,
        string Name,
        string Division,
        string Unit,
        Guid OperatorId,
        string OperatorName,
        DateTimeOffset At,
        List<StockTakeObservationDto> Observations
    ) : QueryBase<UpsertResultDto>;
}
