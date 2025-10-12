using System.Net.Http.Json;
using BOTGC.POS.Models;

namespace BOTGC.POS.Services;

public sealed class HttpStockTakeService : IStockTakeService
{
    private readonly IHttpClientFactory _factory;

    public HttpStockTakeService(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<StockTakeDivisionPlan>> GetPlannedAsync()
    {
        var client = _factory.CreateClient("Api");
        var dto = await client.GetFromJsonAsync<List<DivisionPlanDto>>("/api/stock/stockTakes/products");
        if (dto == null) return Array.Empty<StockTakeDivisionPlan>();

        return dto.Select(d => new StockTakeDivisionPlan
        {
            Division = d.Division,
            Products = d.Products?.Select(p => new StockTakeProduct
            {
                StockItemId = p.StockItemId,
                Name = p.Name,
                Unit = p.Unit,
                Division = p.Division,
                CurrentQuantity = p.CurrentQuantity,
                LastStockTake = p.LastStockTake,
                DaysSinceLastStockTake = p.DaysSinceLastStockTake,
                StockTakes = p.StockTakes?.Select(s => new StockTakeSnapshot
                {
                    Timestamp = s.Timestamp,
                    Before = s.Before,
                    After = s.After,
                    Adjustment = s.Adjustment,
                    StockRoomId = s.StockRoomId
                }).ToList() ?? new List<StockTakeSnapshot>()
            }).ToList() ?? new List<StockTakeProduct>()
        }).ToList();
    }

    public async Task<IReadOnlyList<StockTakeDraftEntry>> GetSheetAsync(DateOnly day, string division)
    {
        var client = _factory.CreateClient("Api");
        var date = day.ToString("yyyy-MM-dd");

        var sheet = await client.GetFromJsonAsync<ApiStockTakeSheetDto>(
            $"/api/stock/stockTakes/sheet?day={date}&division={Uri.EscapeDataString(division)}"
        );

        var entries = sheet?.Entries ?? new List<ApiStockTakeEntryDto>();

        return entries.Select(e => new StockTakeDraftEntry
        {
            StockItemId = e.StockItemId,
            Name = e.Name,
            Division = e.Division,
            Unit = e.Unit,
            OperatorId = e.OperatorId,
            OperatorName = e.OperatorName,
            At = e.At,
            Observations = e.Observations.Select(o => new StockTakeObservation
            {
                StockItemId = o.StockItemId,
                Code = o.Code,
                Location = o.Location,
                Value = o.Value
            }).ToList()
        }).ToList();
    }

    public async Task UpsertEntryAsync(DateOnly day, StockTakeDraftEntry entry)
    {
        var client = _factory.CreateClient("Api");
        var date = day.ToString("yyyy-MM-dd");

        var payload = new ApiStockTakeEntryRequest(
            entry.StockItemId,
            entry.Name,
            entry.Division,
            entry.Unit,
            entry.OperatorId,
            entry.OperatorName,
            entry.At,
            entry.Observations.Select(o => new ApiStockTakeObservationRequest(o.StockItemId, o.Code, o.Location, o.Value)).ToList(),
            entry.EstimatedQuantityAtCapture
        );

        var res = await client.PostAsJsonAsync($"/api/stock/stockTakes/sheet/entry?day={date}", payload);
        res.EnsureSuccessStatusCode();
    }

    public async Task RemoveEntryAsync(DateOnly day, string division, int stockItemId)
    {
        var client = _factory.CreateClient("Api");
        var date = day.ToString("yyyy-MM-dd");
        var res = await client.DeleteAsync($"/api/stock/stockTakes/sheet/entry/{stockItemId}?day={date}&division={Uri.EscapeDataString(division)}");
        res.EnsureSuccessStatusCode();
    }

    // ---------- API DTOs (prefixed Api* to avoid clashes) ----------

    private sealed record DivisionPlanDto(string Division, List<ProductDto> Products);

    private sealed record ProductDto(
        int StockItemId,
        string Name,
        string Unit,
        string Division,
        decimal? CurrentQuantity,
        DateTimeOffset? LastStockTake,
        int? DaysSinceLastStockTake,
        List<SnapshotDto> StockTakes
    );

    private sealed record SnapshotDto(
        DateTimeOffset Timestamp,
        decimal? Before,
        decimal? After,
        decimal? Adjustment,
        int? StockRoomId
    );

    private sealed record ApiStockTakeObservationRequest(int StockItemId, string Code, string? Location, decimal Value);

    private sealed record ApiStockTakeEntryRequest(
        int StockItemId,
        string Name,
        string Division,
        string Unit,
        Guid OperatorId,
        string OperatorName,
        DateTimeOffset At,
        List<ApiStockTakeObservationRequest> Observations, 
        decimal EstimatedQuantityAtCapture
    );

    private sealed record ApiStockTakeSheetDto(
        DateTime Date,
        string Division,
        string Status,
        List<ApiStockTakeEntryDto> Entries
    );

    private sealed record ApiStockTakeEntryDto(
        int StockItemId,
        string Name,
        string Division,
        string Unit,
        Guid OperatorId,
        string OperatorName,
        DateTimeOffset At,
        List<ApiStockTakeObservationDto> Observations
    );

    private sealed record ApiStockTakeObservationDto(
        int StockItemId,
        string Code,
        string? Location,
        decimal Value
    );
}
