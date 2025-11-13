using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries;

public sealed record StockTakeCompletedCommand(
    int? StockTakeId, 
    DateTime Date,
    string Division,
    IReadOnlyList<StockTakeItemInvestigationDto> InvestigateItems,
    IReadOnlyList<StockTakeItemAcceptedDto> AcceptedItems,
    string CorrelationId, 
    string Summary
) : QueryBase<bool>;
