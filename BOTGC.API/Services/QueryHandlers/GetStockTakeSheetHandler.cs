using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public partial class GetStockTakeSheetHandler
    {
        public class GetHandler(IOptions<AppSettings> settings,
                                ILogger<GetHandler> logger,
                                IServiceScopeFactory serviceScopeFactory,
                                IMediator mediator)
            : QueryHandlerBase<GetStockTakeSheetQuery, StockTakeSheetDto>
        {
            private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            private readonly ILogger<GetHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

            public async override Task<StockTakeSheetDto> Handle(GetStockTakeSheetQuery request, CancellationToken cancellationToken)
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

                static string CacheKey(DateTime d, string div) => $"StockTakeSheet:{d:yyyyMMdd}:{div?.Trim() ?? string.Empty}";

                var date = request.Date.Date;
                var division = request.Division?.Trim() ?? string.Empty;
                var key = CacheKey(date, division);

                var sheet = await cache.GetAsync<StockTakeSheetCacheModel>(key).ConfigureAwait(false);

                if (sheet is null)
                {
                    _logger.LogInformation("No stock take sheet in cache for {Date} / {Division}. Seeding.", date.ToString("yyyy-MM-dd"), division);

                    // Get the pre-selected product list (your existing selection logic).
                    var suggestions = await _mediator.Send(new GetStockTakeProductsQuery(), cancellationToken);
                    var div = suggestions.FirstOrDefault(x => string.Equals(x.Division, division, StringComparison.OrdinalIgnoreCase));

                    var entries = new Dictionary<int, StockTakeEntryDto>();
                    if (div is not null)
                    {
                        foreach (var p in div.Products)
                        {
                            entries[p.StockItemId] = new StockTakeEntryDto(
                                StockItemId: p.StockItemId,
                                Name: p.Name,
                                Division: p.Division,
                                Unit: p.Unit,
                                OperatorId: Guid.Empty,
                                OperatorName: string.Empty,
                                At: default,
                                Observations: new List<StockTakeObservationDto>(),
                                EstimatedQuantityAtCapture: p.CurrentQuantity ?? 0m  
                            );
                        }
                    }

                    sheet = new StockTakeSheetCacheModel
                    {
                        Date = date,
                        Division = division,
                        Status = "Open",
                        Entries = entries
                    };

                    await cache.SetAsync(key, sheet, TimeSpan.FromDays(180)).ConfigureAwait(false);
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
