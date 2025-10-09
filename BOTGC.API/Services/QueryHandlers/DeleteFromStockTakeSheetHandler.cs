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

                var CacheKey = (DateTime d, string div) => $"StockTakeSheet:{d:yyyyMMdd}:{div?.Trim() ?? string.Empty}";

                var date = request.Date.Date;
                var division = request.Division?.Trim() ?? string.Empty;
                var key = CacheKey(date, division);
                var ttl = TimeSpan.FromDays(180);

                for (var attempt = 0; attempt < 3; attempt++)
                {
                    var sheet = await cache.GetAsync<StockTakeSheetCacheModel>(key).ConfigureAwait(false);

                    if (sheet is null)
                    {
                        _logger.LogInformation("No stock take sheet present for {Date} / {Division}. Nothing to delete.", date.ToString("yyyy-MM-dd"), division);
                        return new DeleteResultDto(false);
                    }

                    var found = sheet.Entries.Remove(request.StockItemId);
                    await cache.SetAsync(key, sheet, ttl).ConfigureAwait(false);

                    var check = await cache.GetAsync<StockTakeSheetCacheModel>(key).ConfigureAwait(false);
                    var stillThere = check?.Entries.ContainsKey(request.StockItemId) == true;

                    if (!stillThere)
                    {
                        if (found)
                        {
                            _logger.LogInformation("Deleted stock take entry {StockItemId} for {Date} / {Division}.",
                                request.StockItemId, date.ToString("yyyy-MM-dd"), division);
                            return new DeleteResultDto(true);
                        }
                        _logger.LogInformation("Entry {StockItemId} not found for {Date} / {Division}.", request.StockItemId, date.ToString("yyyy-MM-dd"), division);
                        return new DeleteResultDto(false);
                    }
                }

                _logger.LogWarning("After retries, entry {StockItemId} may still be present for {Date} / {Division}.", request.StockItemId, date.ToString("yyyy-MM-dd"), division);
                return new DeleteResultDto(false);
            }
        }
    }
}
