using Microsoft.AspNetCore.SignalR;

namespace BOTGC.POS.Hubs
{
    public sealed class WastageHub : Hub { }

    public sealed record EntryAddedMessage(
        Guid Id,
        string AtIso,
        Guid OperatorId,
        string OperatorName,
        Guid ProductId,
        string ProductName,
        string Reason,
        decimal Quantity
    );

    public sealed record SheetSubmittedMessage(
        string TradingDateIso,
        Guid SubmittedBy
    );
}
