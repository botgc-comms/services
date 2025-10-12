using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public partial class DeleteFromStockTakeSheetHandler
    {
        public class DeleteHandler(IOptions<AppSettings> settings,
                                   ILogger<DeleteHandler> logger,
                                   IServiceScopeFactory serviceScopeFactory)
            : QueryHandlerBase<DeleteFromStockTakeSheetCommand, DeleteResultDto>
        {
            private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            private readonly ILogger<DeleteHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

            public async override Task<DeleteResultDto> Handle(DeleteFromStockTakeSheetCommand request, CancellationToken cancellationToken)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

                static string CacheKey(DateTime d, string div) => $"StockTakeSheet:{d:yyyyMMdd}:{div?.Trim() ?? string.Empty}";

                var date = request.Date.Date;
                var division = request.Division?.Trim() ?? string.Empty;
                var key = CacheKey(date, division);
                var ttl = TimeSpan.FromDays(180);

                for (var attempt = 0; attempt < 3; attempt++)
                {
                    var sheet = await cache.GetAsync<StockTakeSheetCacheModel>(key).ConfigureAwait(false);

                    if (sheet is null)
                    {
                        _logger.LogInformation("No stock take sheet present for {Date} / {Division}. Nothing to clear.", date.ToString("yyyy-MM-dd"), division);
                        return new DeleteResultDto(false);
                    }

                    if (!sheet.Entries.TryGetValue(request.StockItemId, out var existing))
                    {
                        _logger.LogInformation("Entry {StockItemId} not found for {Date} / {Division}.", request.StockItemId, date.ToString("yyyy-MM-dd"), division);
                        return new DeleteResultDto(false);
                    }

                    // Clear observations & operator metadata, but KEEP the seeded estimate and identity.
                    var cleared = new StockTakeEntryDto(
                        StockItemId: existing.StockItemId,
                        Name: existing.Name,
                        Division: existing.Division,
                        Unit: existing.Unit,
                        OperatorId: Guid.Empty,
                        OperatorName: string.Empty,
                        At: default,
                        Observations: new List<StockTakeObservationDto>(),
                        EstimatedQuantityAtCapture: existing.EstimatedQuantityAtCapture
                    );

                    sheet.Entries[request.StockItemId] = cleared;

                    await cache.SetAsync(key, sheet, ttl).ConfigureAwait(false);

                    // Verify write
                    var check = await cache.GetAsync<StockTakeSheetCacheModel>(key).ConfigureAwait(false);
                    if (check != null &&
                        check.Entries.TryGetValue(request.StockItemId, out var persisted) &&
                        persisted.Observations.Count == 0 &&
                        persisted.OperatorId == Guid.Empty)
                    {
                        _logger.LogInformation("Cleared observations for entry {StockItemId} on {Date} / {Division}.",
                            request.StockItemId, date.ToString("yyyy-MM-dd"), division);
                        return new DeleteResultDto(true);
                    }
                }

                _logger.LogWarning("After retries, entry {StockItemId} may not have been cleared for {Date} / {Division}.",
                    request.StockItemId, date.ToString("yyyy-MM-dd"), division);
                return new DeleteResultDto(false);
            }
        }
    }
}
