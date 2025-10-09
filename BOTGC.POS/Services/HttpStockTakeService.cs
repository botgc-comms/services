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

        var result = dto.Select(d => new StockTakeDivisionPlan
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

        return result;
    }

    public async Task<bool> CommitAsync(Guid operatorId, DateTimeOffset timestamp, IEnumerable<StockTakeObservationDto> observations)
    {
        var client = _factory.CreateClient("Api");
        var payload = new CommitRequest(
            operatorId,
            timestamp,
            observations.Select(o => new CommitObservation(o.StockItemId, o.Code, o.Location, o.Value)).ToList()
        );

        var res = await client.PostAsJsonAsync("/api/stock/stockTakes/commit", payload);
        return res.IsSuccessStatusCode;
    }

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

    private sealed record CommitObservation(int StockItemId, string Code, string? Location, decimal Value);
    private sealed record CommitRequest(Guid OperatorId, DateTimeOffset Timestamp, List<CommitObservation> Observations);
}
