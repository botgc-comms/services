using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetMembershipReportHandler(IOptions<AppSettings> settings,
                                            ILogger<GetMembershipReportHandler> logger,
                                            IDataProvider dataProvider,
                                            IReportParser<MemberDto> reportParser) : QueryHandlerBase<GetMembershipReportQuery, List<MemberDto>>
    {
        private const string __CACHE_KEY = "Membership_Report";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<GetMembershipReportHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParser<MemberDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

        public async override Task<List<MemberDto>> Handle(GetMembershipReportQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = __CACHE_KEY;

            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.MembershipReportingUrl}";
            var members = await _dataProvider.GetData<MemberDto>(reportUrl, _reportParser, cacheKey, TimeSpan.FromMinutes(_settings.Cache.LongTerm_TTL_mins));

            _logger.LogInformation($"Successfully retrieved the {members.Count} member records.");

            return members;
        }
    }
}
