using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public class GetStockTakesListHandler(IOptions<AppSettings> settings,
                                      ILogger<GetStockTakesHandler> logger,
                                      IDataProvider dataProvider,
                                      IReportParser<StockTakeDto> reportParser)
    : QueryHandlerBase<GetStockTakesListQuery, List<StockTakeDto>>
{
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<GetStockTakesHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    private readonly IReportParser<StockTakeDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

    public async override Task<List<StockTakeDto>?> Handle(GetStockTakesListQuery request, CancellationToken cancellationToken)
    {
        var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.StockTakesListReportUrl}";
        var stockItems = await _dataProvider.GetData<StockTakeDto>(reportUrl, _reportParser);

        if (stockItems != null && stockItems.Any())
        {
            _logger.LogInformation($"Successfully retrieved {stockItems.Count} stock takes.");
        }

        return stockItems;

    }
}


