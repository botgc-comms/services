using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Humanizer;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetMobileOrdersForDateHandler(IOptions<AppSettings> settings,
                                                ILogger<GetMobileOrdersForDateHandler> logger,
                                                IDataProvider dataProvider,
                                                IReportParser<SecurityLogEntryDto> reportParser) : QueryHandlerBase<GetMobileOrdersForDateQuery, List<SecurityLogEntryDto>?>
    {
        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<GetMobileOrdersForDateHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParser<SecurityLogEntryDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

        public async override Task<List<SecurityLogEntryDto>?> Handle(GetMobileOrdersForDateQuery request, CancellationToken cancellationToken)
        {
            var forDate = request.ForDate ?? DateTime.Now.Date;

            var securityLogUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.SecurityLogMobileOrders}".Replace("{today}", forDate.ToString("dd-MM-yyyy"));
            var securityLog = await _dataProvider.GetData<SecurityLogEntryDto>(securityLogUrl, _reportParser);

            if (securityLog != null && securityLog.Any())
            {
                var deduplicated = new List<SecurityLogEntryDto>();

                SecurityLogEntryDto? previous = null;
                foreach (var current in securityLog)
                {
                    if (previous == null || current.Event != previous.Event)
                    {
                        deduplicated.Add(current);
                    }
                    previous = current;
                }

                _logger.LogInformation($"Successfully retrieved {deduplicated.Count} mobile orders.");

                return deduplicated;
            }

            return null;
        }
    }
}
