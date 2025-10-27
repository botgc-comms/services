using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetScorecardForRoundHandler(IOptions<AppSettings> settings,
                                             ILogger<GetScorecardForRoundHandler> logger,
                                             IDataProvider dataProvider,
                                             IReportParser<ScorecardDto> reportParser) : QueryHandlerBase<GetScorecardForRoundQuery, ScorecardDto?>
    {
        private const string __CACHE_KEY = "Scorecard_By_Round:{roundId}";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<GetScorecardForRoundHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParser<ScorecardDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

        public async override Task<ScorecardDto?> Handle(GetScorecardForRoundQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = __CACHE_KEY.Replace("{roundId}", request.RoundId);

            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.RoundReportUrl.Replace("{roundId}", request.RoundId)}";
            var scorecards = await _dataProvider.GetData<ScorecardDto>(reportUrl, _reportParser, cacheKey, TimeSpan.FromMinutes(_settings.Cache.LongTerm_TTL_mins));

            if (scorecards != null && scorecards.Any())
            {
                _logger.LogInformation($"Successfully retrieved the scorecard for round {request.RoundId}.");

                return scorecards.FirstOrDefault();
            }

            return null;
        }
    }
}
