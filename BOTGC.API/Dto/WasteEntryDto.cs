namespace BOTGC.API.Dto
{
    public sealed record WasteEntryDto(
        Guid ClientEntryId,
        DateTimeOffset CreatedAtUtc,
        Guid OperatorId,
        Guid ProductId,
        long IGProductId,
        string Unit,
        string ProductName,
        string Reason,
        decimal Quantity
    );
}
