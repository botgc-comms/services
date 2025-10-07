namespace BOTGC.API.Models
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
