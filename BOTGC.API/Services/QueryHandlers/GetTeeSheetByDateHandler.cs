using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetTeeSheetByDateHandler(IOptions<AppSettings> settings,
                                          ILogger<GetTeeSheetByDateHandler> logger,
                                          IDataProvider dataProvider,
                                          IReportParser<TeeSheetDto> reportParser) : QueryHandlerBase<GetTeeSheetByDateQuery, TeeSheetDto?>
    {
        private const string __CACHE_KEY = "TeeSheet_By_Date_{date}";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<GetTeeSheetByDateHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParser<TeeSheetDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

        public async override Task<TeeSheetDto?> Handle(GetTeeSheetByDateQuery request, CancellationToken cancellationToken)
        {
            var cacheKey = __CACHE_KEY.Replace("{date}", request.Date.ToString("yyyy-MM-dd"));
            var isToday = request.Date.Date == DateTime.Today;

            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.TeeBookingsUrl}".Replace("{date}", request.Date.ToString("dd-MM-yyyy"));
            var teesheet = await _dataProvider.GetData<TeeSheetDto>(reportUrl, _reportParser, cacheKey, TimeSpan.FromMinutes(isToday ? _settings.Cache.ShortTerm_TTL_mins : _settings.Cache.Forever_TTL_Mins));

            if (teesheet != null && teesheet.Any())
            {
                _logger.LogInformation($"Successfully retrieved the teesheet for {request.Date.ToString("dd MM yyyy")}.");

                return teesheet.FirstOrDefault();
            }

            return null;
        }
    }
}
