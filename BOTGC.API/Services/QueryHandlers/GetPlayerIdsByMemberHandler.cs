using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetPlayerIdsByMemberHandler(IOptions<AppSettings> settings,
                                            ILogger<GetPlayerIdsByMemberHandler> logger,
                                            IDataProvider dataProvider,
                                            IReportParser<PlayerIdLookupDto> reportParser) : QueryHandlerBase<GetPlayerIdsByMemberQuery, List<PlayerIdLookupDto>>
    {
        private const string __CACHE_KEY = "PlayerId_Lookup";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<GetPlayerIdsByMemberHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParser<PlayerIdLookupDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

        public async override Task<List<PlayerIdLookupDto>> Handle(GetPlayerIdsByMemberQuery request, CancellationToken cancellationToken)
        {
            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.PlayerIdLookupReportUrl}";
            var playerIdLookup = await _dataProvider.GetData<PlayerIdLookupDto>(reportUrl, _reportParser, __CACHE_KEY);

            _logger.LogInformation($"Retrieved {playerIdLookup.Count()} player lookup records.");

            return playerIdLookup;
        }
    }
}
