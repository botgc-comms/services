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
        private const string __CACHE_KEY = "Management_Report";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<GetMembershipReportHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParser<MemberDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

        public async override Task<List<MemberDto>> Handle(GetMembershipReportQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = __CACHE_KEY + "_" + DateTime.Now.ToString("yyyy-MM-dd");

            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.MembershipReportingUrl}";

            // Calculate the time remaining until midnight
            var now = DateTime.Now;
            var midnight = now.Date.AddDays(1);
            var timeUntilMidnight = midnight - now;

            var members = await _dataProvider.GetData<MemberDto>(
                reportUrl,
                _reportParser,
                cacheKey,
                timeUntilMidnight
            );

            _logger.LogInformation($"Successfully retrieved the {members.Count} member records.");

            members = members.Where(m => m.MembershipCategory?.ToLower() != "tests").ToList();

            return members;
        }
    }
}
