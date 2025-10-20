using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public sealed record GetStockItemUnitsQuery(int StockItemId) : QueryBase<IReadOnlyList<StockItemUnitInfoDto>>;
}
