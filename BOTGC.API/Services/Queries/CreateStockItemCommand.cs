using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public sealed record CreateStockItemCommand(string Name, string BaseUnit, string Division, decimal? MinAlert, decimal? MaxAlert, List<StockItemUnitInfoDto>? TradeUnits) : QueryBase<int?>;
}