namespace BOTGC.API.Dto
{
    public sealed record StockTakeCompletedTicketCommandDto(
        DateTime Date,
        string Division,
        IReadOnlyList<StockTakeReportItemDto> InvestigateItems,
        IReadOnlyList<StockTakeReportItemDto> AcceptedItems,
        string Summary
    );
}
