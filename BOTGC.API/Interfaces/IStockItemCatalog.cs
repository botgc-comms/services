using BOTGC.API.Models;

namespace BOTGC.API.Interfaces
{
    public interface IStockItemCatalog
    {
        Task<StockItemConfig?> GetAsync(int stockItemId, CancellationToken ct);
    }
}