namespace BOTGC.POS.Models
{
    public sealed record WasteEntry(
        Guid Id,
        DateTimeOffset At,
        Guid OperatorId,
        Guid ProductId,
        long IGProductId, 
        string Unit, 
        string ProductName,
        string Reason,
        decimal Quantity
    );
}
