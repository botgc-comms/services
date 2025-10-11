namespace BOTGC.API.Dto
{
    public sealed record StockTakeSheetProcessCommandDto(
        StockTakeSheetDto Sheet,
        string CorrelationId,
        DateTime EnqueuedAtUtc
    );

}
