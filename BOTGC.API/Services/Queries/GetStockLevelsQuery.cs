using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public record GetStockLevelsQuery : QueryBase<List<StockItemDto>>;
}
