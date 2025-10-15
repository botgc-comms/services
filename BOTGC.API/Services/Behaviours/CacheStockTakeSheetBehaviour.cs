using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;
using MediatR;

namespace BOTGC.API.Services.Behaviours;

public sealed class CacheStockTakeSheetBehaviour
    : IPipelineBehavior<GetStockTakeSheetQuery, StockTakeSheetDto>
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CacheStockTakeSheetBehaviour> _log;

    public CacheStockTakeSheetBehaviour(IServiceScopeFactory scopeFactory, ILogger<CacheStockTakeSheetBehaviour> log)
    {
        _scopeFactory = scopeFactory;
        _log = log;
    }

    public async Task<StockTakeSheetDto> Handle(
        GetStockTakeSheetQuery request,
        RequestHandlerDelegate<StockTakeSheetDto> next,
        CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        static string CacheKey(DateTime d, string div) => $"StockTakeSheet:{d:yyyyMMdd}:{(div ?? string.Empty).Trim()}";

        var date = request.Date.Date;
        var division = request.Division?.Trim() ?? string.Empty;
        var key = CacheKey(date, division);

        var cached = await cache.GetAsync<StockTakeSheetCacheModel>(key).ConfigureAwait(false);
        if (cached is not null)
        {
            _log.LogDebug("Cache hit for {Key}", key);
            return new StockTakeSheetDto(
                cached.Date == default ? date : cached.Date,
                cached.Division ?? division,
                cached.Status ?? "Open",
                cached.Entries.Values.OrderBy(e => e.Name).ToList()
            );
        }

        _log.LogDebug("Cache miss for {Key}", key);
        var sheet = await next();

        var model = new StockTakeSheetCacheModel
        {
            Date = sheet.Date == default ? date : sheet.Date,
            Division = sheet.Division ?? division,
            Status = sheet.Status ?? "Open",
            // Store the enriched entries (includes Calibration if present)
            Entries = sheet.Entries.ToDictionary(e => e.StockItemId, e => e)
        };

        await cache.SetAsync(key, model, TimeSpan.FromDays(180)).ConfigureAwait(false);
        _log.LogInformation("Cached stock take sheet for {Key}", key);

        return sheet;
    }
}
