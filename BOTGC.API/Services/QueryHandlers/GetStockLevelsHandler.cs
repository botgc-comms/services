using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetStockLevelsHandler(IOptions<AppSettings> settings,
                                       ILogger<GetStockLevelsHandler> logger,
                                       IDataProvider dataProvider,
                                       IReportParser<StockItemDto> reportParser) : QueryHandlerBase<GetStockLevelsQuery, List<StockItemDto>>
    {
        private const string __CACHE_KEY = "Stock_Levels";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<GetStockLevelsHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParser<StockItemDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

        public async override Task<List<StockItemDto>> Handle(GetStockLevelsQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = __CACHE_KEY;

            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.StockItemsUrl}";
            var stockItems = await _dataProvider.GetData<StockItemDto>(reportUrl, _reportParser, cacheKey, TimeSpan.FromMinutes(_settings.Cache.ShortTerm_TTL_mins));

            if (stockItems != null && stockItems.Any())
            {
                _logger.LogInformation($"Successfully retrieved the stock list.");
            }
             
            return stockItems;
        }
    }
}
