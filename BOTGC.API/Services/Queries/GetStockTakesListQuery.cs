using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public sealed record GetStockTakesListQuery: QueryBase<List<StockTakeDto>?>;
}
