using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetTillOperatorsHandler(IOptions<AppSettings> settings,
                                         ILogger<GetTillOperatorsHandler> logger,
                                         IDataProvider dataProvider,
                                         IReportParser<TillOperatorDto> reportParser) : QueryHandlerBase<GetTillOperatorsQuery, List<TillOperatorDto>>
    {
        private const string __CACHE_KEY = "TillOperators";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<GetTillOperatorsHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParser<TillOperatorDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

        public async override Task<List<TillOperatorDto>> Handle(GetTillOperatorsQuery request, CancellationToken cancellationToken)
        {
            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.TillOperatorsReportUrl}";
            var tillOperators = await _dataProvider.GetData<TillOperatorDto>(reportUrl, _reportParser, __CACHE_KEY, TimeSpan.FromMinutes(_settings.Cache.MediumTerm_TTL_mins));

            _logger.LogInformation($"Retrieved {tillOperators.Count()} till operator records.");

            return tillOperators;
        }
    }
}
