using BOTGC.POS.Models;

namespace BOTGC.POS.Services;

public interface IStockTakeService
{
    Task<IReadOnlyList<StockTakeDivisionPlan>> GetPlannedAsync();
    Task<IReadOnlyList<StockTakeDraftEntry>> GetSheetAsync(DateOnly day, string division);
    Task UpsertEntryAsync(DateOnly day, StockTakeDraftEntry entry);
    Task RemoveEntryAsync(DateOnly day, string division, int stockItemId);
}
