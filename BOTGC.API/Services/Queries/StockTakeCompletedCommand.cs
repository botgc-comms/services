using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public sealed record StockTakeCompletedCommand(
        DateTime Date,
        string Division,
        IReadOnlyList<StockTakeItemInvestigationDto> InvestigateItems,
        IReadOnlyList<StockTakeItemAcceptedDto> AcceptedItems,
        string CorrelationId, 
        string Summary
    ) : QueryBase<bool>;
}
