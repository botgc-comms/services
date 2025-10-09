using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public sealed record GetStockTakeSheetQuery(
        DateTime Date,
        string Division
    ) : QueryBase<StockTakeSheetDto>;
}
