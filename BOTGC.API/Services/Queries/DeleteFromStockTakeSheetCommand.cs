using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public sealed record DeleteFromStockTakeSheetCommand(
        DateTime Date,
        string Division,
        int StockItemId
    ) : QueryBase<DeleteResultDto>;
}
