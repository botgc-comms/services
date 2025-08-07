using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetSubscriptionPaymentsByDateRangeHandler(IOptions<AppSettings> settings,
                                                           ILogger<GetSubscriptionPaymentsByDateRangeHandler> logger,
                                                           IDataProvider dataProvider,
                                                           IReportParser<SubscriptionPaymentDto> reportParser) : QueryHandlerBase<GetSubscriptionPaymentsByDateRangeQuery, List<SubscriptionPaymentDto>>
    {
        private const string __CACHE_KEY = "Membership_Subscriptions_{fromDate}_{toDate}";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<GetSubscriptionPaymentsByDateRangeHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParser<SubscriptionPaymentDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

        public async override Task<List<SubscriptionPaymentDto>> Handle(GetSubscriptionPaymentsByDateRangeQuery request, CancellationToken cancellationToken)
        {
            var fromDate = request.FromDate;
            var toDate = request.ToDate;

            var cacheKey = __CACHE_KEY.Replace("{fromDate}", fromDate.ToString("yyyy-MM-dd")).Replace("{toDate}", toDate.ToString("yyyy-MM-dd"));

            var daterange = $"{fromDate.ToString("dd/MM/yyyy")} - {toDate.ToString("dd/MM/yyyy")}";
            var reportName = "Bills Raised";

            var data = new Dictionary<string, string>
            {
                { "daterange", daterange },
                { "datetouse", "bill" },
                { "groupinstalments", "1" },
                { "breakdown", "items" },
                { "pdftitle", $"All bills due {daterange}" },
                { "reportname", reportName }
            };

            var reportUrl = $"{_settings.IG.BaseUrl}/membership_reports.php?tab=chargedreport&requestType=ajax&ajaxaction=getreport";
            var subscriptionPayments = await _dataProvider.PostData<SubscriptionPaymentDto>(
                reportUrl,
                data,
                _reportParser,
                cacheKey,
                TimeSpan.FromMinutes(_settings.Cache.MediumTerm_TTL_mins));

            _logger.LogInformation($"Successfully retrieved the {subscriptionPayments.Count} subscription payment records.");

            return subscriptionPayments;
        }
    }
}
