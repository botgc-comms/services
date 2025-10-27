using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BOTGC.API.Services.QueryHandlers
{
    public class LookupMemberCDHIdDetailsHandler(IOptions<AppSettings> settings,
                                                 ILogger<LookupMemberCDHIdDetailsHandler> logger,
                                                 IDataProvider dataProvider,
                                                 IReportParser<MemberCDHLookupDto> reportParser) : QueryHandlerBase<LookupMemberCDHIdDetailsQuery, MemberCDHLookupDto?>
    {
        private const string __CACHE_KEY = "MemberCDHLookup:{cdhid}";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<LookupMemberCDHIdDetailsHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParser<MemberCDHLookupDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

        public async override Task<MemberCDHLookupDto?> Handle(LookupMemberCDHIdDetailsQuery request, CancellationToken cancellationToken)
        {
            var url = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.MemberCDHLookupUrl}";
            var cacheKey = __CACHE_KEY.Replace("{cdhid}", request.CDHId);

            var data = new Dictionary<string, string>(
            [
                new KeyValuePair<string, string>("cdh_id_lookup", request.CDHId)
            ]);

            var result = await _dataProvider.PostData(url, data, _reportParser, cacheKey, TimeSpan.FromMinutes(_settings.Cache.ShortTerm_TTL_mins));

            if (result != null && result.Any())
            {
                return result.FirstOrDefault();
            }

            return null;
        }
    }
}
