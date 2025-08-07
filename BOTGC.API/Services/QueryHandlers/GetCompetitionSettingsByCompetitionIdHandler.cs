using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetCompetitionSettingsByCompetitionIdHandler(IOptions<AppSettings> settings,
                                                              ILogger<GetCompetitionSettingsByCompetitionIdHandler> logger,
                                                              IDataProvider dataProvider,
                                                              IReportParser<CompetitionSettingsDto> competitionSettingsReportParser,
                                                              IReportParser<CompetitionSummaryDto> competitionSummaryReportParser) : QueryHandlerBase<GetCompetitionSettingsByCompetitionIdQuery, CompetitionSettingsDto?>
    {
        private const string __CACHE_COMPETITIONSETTINGS = "Competition_Settings_{compid}";
        private const string __CACHE_COMPETITIONSUMMARY = "Competition_Summary_{compid}";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<GetCompetitionSettingsByCompetitionIdHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParser<CompetitionSettingsDto> _competitionSettingsReportParser = competitionSettingsReportParser ?? throw new ArgumentNullException(nameof(competitionSettingsReportParser));
        private readonly IReportParser<CompetitionSummaryDto> _competitionSummaryReportParser = competitionSummaryReportParser ?? throw new ArgumentNullException(nameof(competitionSummaryReportParser));

        public async override Task<CompetitionSettingsDto?> Handle(GetCompetitionSettingsByCompetitionIdQuery request, CancellationToken cancellationToken)
        {
            var competitionSettingsUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.CompetitionSettingsUrl}".Replace("{compid}", request.CompetitionId);
            var competitionSettings = await _dataProvider.GetData<CompetitionSettingsDto>(competitionSettingsUrl, _competitionSettingsReportParser, __CACHE_COMPETITIONSETTINGS.Replace("{compid}", request.CompetitionId), TimeSpan.FromMinutes(_settings.Cache.Default_TTL_Mins));

            var competitionSummaryUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.CompetitionSummaryUrl}".Replace("{compid}", request.CompetitionId);
            var competitionSummary = await _dataProvider.GetData<CompetitionSummaryDto>(competitionSummaryUrl, _competitionSummaryReportParser, __CACHE_COMPETITIONSUMMARY.Replace("{compid}", request.CompetitionId), TimeSpan.FromMinutes(_settings.Cache.Default_TTL_Mins));

            if (competitionSettings != null && competitionSettings.Any())
            {
                _logger.LogInformation($"Successfully retrieved the competition settings or {request.CompetitionId}.");

                var retVal = competitionSettings.FirstOrDefault();
                if (retVal != null)
                {
                    retVal.MultiPartCompetition = competitionSummary.FirstOrDefault()?.MultiPartCompetition;
                }

                return retVal;
            }

            return null;
        }
    }
}
