using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public class GetTillProductInformationHandler(IOptions<AppSettings> settings,
                                              ILogger<GetCompetitionLeaderboardByCompetitionHandler> logger,
                                              IDataProvider dataProvider,
                                              IReportParser<TillProductInformationDto> reportParser) : QueryHandlerBase<GetTillProductInformationQuery, TillProductInformationDto?>
{
    private const string __CACHE_KEY = "Till_Product_{productid}";

    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<GetCompetitionLeaderboardByCompetitionHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    private readonly IReportParser<TillProductInformationDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

    public async override Task<TillProductInformationDto?> Handle(GetTillProductInformationQuery request, CancellationToken cancellationToken)
    {
        var productId = request.ProductId.ToString();

        var productSettingsUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.TillProductInformationUrl}".Replace("{productid}", productId);
        var productInformation = await _dataProvider.GetData<TillProductInformationDto>(productSettingsUrl, _reportParser, __CACHE_KEY.Replace("{productid}", productId.ToString()), TimeSpan.FromMinutes(_settings.Cache.LongTerm_TTL_mins));

        if (productInformation != null && productInformation.Any())
        {
            _logger.LogInformation($"Successfully retrieved product information for product {productId}.");

            return productInformation.FirstOrDefault();
        }

        return null;
    }
}
