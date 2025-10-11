using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public partial class UpsertStockTakeEntryHandler
    {
        public class UpsertHandler(IOptions<AppSettings> settings,
                                   ILogger<UpsertHandler> logger,
                                   IServiceScopeFactory serviceScopeFactory)
            : QueryHandlerBase<UpsertStockTakeEntryCommand, UpsertResultDto>
        {
            private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            private readonly ILogger<UpsertHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

            public async override Task<UpsertResultDto> Handle(UpsertStockTakeEntryCommand request, CancellationToken cancellationToken)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

                var CacheKey = (DateTime d, string div) => $"StockTakeSheet:{d:yyyyMMdd}:{div?.Trim() ?? string.Empty}";
                var date = request.Date.Date;
                var division = request.Division?.Trim() ?? string.Empty;
                var key = CacheKey(date, division);
                var ttl = TimeSpan.FromDays(180);

                for (var attempt = 0; attempt < 3; attempt++)
                {
                    var sheet = await cache.GetAsync<StockTakeSheetCacheModel>(key).ConfigureAwait(false)
                                ?? new StockTakeSheetCacheModel { Date = date, Division = division, Status = "Open" };

                    var entry = new StockTakeEntryDto(
                        request.StockItemId,
                        request.Name,
                        division,
                        request.Unit,
                        request.OperatorId,
                        request.OperatorName,
                        request.At,
                        request.Observations?.ToList() ?? new List<StockTakeObservationDto>(), 
                        request.EstimatedQuantityAtCapture
                    );

                    sheet.Status = "Open";
                    sheet.Date = date;
                    sheet.Division = division;
                    sheet.Entries[request.StockItemId] = entry;

                    await cache.SetAsync(key, sheet, ttl).ConfigureAwait(false);

                    var check = await cache.GetAsync<StockTakeSheetCacheModel>(key).ConfigureAwait(false);
                    if (check != null && check.Entries.TryGetValue(request.StockItemId, out var persisted))
                    {
                        _logger.LogInformation("Upserted stock take entry {StockItemId} for {Date} / {Division}: {Product}.",
                            request.StockItemId, date.ToString("yyyy-MM-dd"), division, request.Name);
                        return new UpsertResultDto(true);
                    }
                }

                _logger.LogWarning("After retries, stock take entry {StockItemId} may not have persisted for {Date} / {Division}. Treating as success.",
                    request.StockItemId, date.ToString("yyyy-MM-dd"), division);
                return new UpsertResultDto(true);
            }
        }
    }
}
