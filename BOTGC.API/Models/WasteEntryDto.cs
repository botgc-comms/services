namespace BOTGC.API.Models
{
    public sealed record WasteEntryDto(
        Guid ClientEntryId,
        DateTimeOffset CreatedAtUtc,
        Guid OperatorId,
        Guid ProductId,
        string ProductName,
        string Reason,
        decimal Quantity,
        string? DeviceId
    );
}
