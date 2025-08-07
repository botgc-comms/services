using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetMemberEventsHandler(IOptions<AppSettings> settings,
                                        ILogger<GetMemberEventsHandler> logger,
                                        IDataProvider dataProvider,
                                        IReportParser<MemberEventDto> reportParser) : QueryHandlerBase<GetMemberEventsQuery, List<MemberEventDto>>
    {
        private const string __CACHE_KEY = "Membership_Event_History_{fromDate}_{toDate}";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<GetMemberEventsHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParser<MemberEventDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

        public async override Task<List<MemberEventDto>> Handle(GetMemberEventsQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = __CACHE_KEY.Replace("{fromDate}", request.FromDate.ToString("yyyy-MM-dd")).Replace("{toDate}", request.ToDate.ToString("yyyy-MM-dd"));

            var data = new Dictionary<string, string>
            {
                { "layout3", "1" },
                { "daterange", $"{request.FromDate.ToString("dd/MM/yyyy")} - {request.ToDate.ToString("dd/MM/yyyy")}" },
                { "fromDate", request.FromDate.ToString("dd/MM/yyyy") },
                { "toDate", request.ToDate.ToString("dd/MM/yyyy") }
            };

            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.MembershipEventHistoryReportUrl}";
            var events = await _dataProvider.PostData<MemberEventDto>(reportUrl, data, _reportParser, cacheKey, TimeSpan.FromMinutes(_settings.Cache.MediumTerm_TTL_mins));

            _logger.LogInformation($"Successfully retrieved the {events.Count} member event records.");

            return events;
        }
    }
}
