namespace BOTGC.API.Dto
{
    public sealed record StockTakeObservationDto(
        int StockItemId,
        string Code,
        string? Location,
        decimal Value
    );

}
