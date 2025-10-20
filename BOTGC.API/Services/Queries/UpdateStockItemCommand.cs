namespace BOTGC.API.Services.Queries
{
    public sealed record UpdateStockItemCommand(int StockId, string? Name, decimal? MinAlert, decimal? MaxAlert) : QueryBase<bool>;
}