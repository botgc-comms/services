namespace BOTGC.API.Models
{
    public sealed record AddWasteEntryRequest(
        Guid ClientEntryId,
        Guid OperatorId,
        Guid ProductId,
        string ProductName,
        string Reason,
        decimal Quantity,
        string? DeviceId
    );

}
