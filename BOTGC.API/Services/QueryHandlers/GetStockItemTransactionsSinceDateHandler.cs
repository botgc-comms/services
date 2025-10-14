using System.Globalization;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetStockItemTransactionsSinceDateHandler(IOptions<AppSettings> settings,
                                                          IMediator mediator,
                                                          ILogger<GetStockTakesHandler> logger,
                                                          IDataProvider dataProvider,
                                                          IReportParser<StockItemTransactionReportEntryDto> reportParser)
       : QueryHandlerBase<GetStockItemTransactionSinceDateQuery, List<StockItemTransactionReportEntryDto>>
    {
        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<GetStockTakesHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly IReportParser<StockItemTransactionReportEntryDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

        public async override Task<List<StockItemTransactionReportEntryDto>> Handle(GetStockItemTransactionSinceDateQuery request, CancellationToken cancellationToken)
        {
            var tzId = _settings.Waste?.TimeZone ?? "Europe/London";
            var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
            var todayLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz).Date;

            var from = request.FromDate;
            var to = DateTime.Now;

            var form = new Dictionary<string, string>
            {
                { "rangetype", "CU" },
                { "datefrom", from.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) },
                { "timefrom", from.ToString("HH:mm:ss", CultureInfo.InvariantCulture) },
                { "dateto", to.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) },
                { "timeto", "23:59" },
                { "till_config_id", request.TillConfigId.ToString(CultureInfo.InvariantCulture) },
                { "zread", request.ZRead.ToString(CultureInfo.InvariantCulture) }
            };

            var ttl = TimeSpan.FromMinutes(_settings.Cache.MediumTerm_TTL_mins);
            var url = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.GetStockTakesReportUrl}";

            var results = await _dataProvider.PostData<StockItemTransactionReportEntryDto>(
                url,
                form,
                _reportParser,
                null,
                ttl
            ) ?? new List<StockItemTransactionReportEntryDto>();

            return results;
        }
    }
}
