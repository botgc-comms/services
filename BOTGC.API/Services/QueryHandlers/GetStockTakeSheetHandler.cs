using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public partial class GetStockTakeSheetHandler
    {
        public class GetHandler(IOptions<AppSettings> settings,
                                ILogger<GetHandler> logger,
                                IServiceScopeFactory serviceScopeFactory)
            : QueryHandlerBase<GetStockTakeSheetQuery, StockTakeSheetDto>
        {
            private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            private readonly ILogger<GetHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

            public async override Task<StockTakeSheetDto> Handle(GetStockTakeSheetQuery request, CancellationToken cancellationToken)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

                var CacheKey = (DateTime d, string div) => $"StockTakeSheet:{d:yyyyMMdd}:{div?.Trim() ?? string.Empty}";

                var date = request.Date.Date;
                var division = request.Division?.Trim() ?? string.Empty;
                var key = CacheKey(date, division);

                var sheet = await cache.GetAsync<StockTakeSheetCacheModel>(key).ConfigureAwait(false);

                if (sheet is null)
                {
                    _logger.LogInformation("No stock take sheet in cache for {Date} / {Division}. Returning empty.", date.ToString("yyyy-MM-dd"), division);
                    return new StockTakeSheetDto(date, division, "Open", new List<StockTakeEntryDto>());
                }

                return new StockTakeSheetDto(
                    sheet.Date == default ? date : sheet.Date,
                    sheet.Division ?? division,
                    sheet.Status ?? "Open",
                    sheet.Entries.Values.OrderBy(e => e.Name).ToList()
                );
            }
        }
    }
}
