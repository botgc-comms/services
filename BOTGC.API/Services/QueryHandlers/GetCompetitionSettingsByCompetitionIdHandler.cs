using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public class GetCompetitionSettingsByCompetitionIdHandler(IOptions<AppSettings> settings,
                                                          ILogger<GetCompetitionSettingsByCompetitionIdHandler> logger,
                                                          IDataProvider dataProvider,
                                                          IReportParser<CompetitionSettingsDto> competitionSettingsReportParser,
                                                          IReportParser<CompetitionSummaryDto> competitionSummaryReportParser,
                                                          IReportParser<CompetitionSignupSettingsDto> competitionSignupSettingsReportParser) : QueryHandlerBase<GetCompetitionSettingsByCompetitionIdQuery, CompetitionSettingsDto?>
{
    private const string __CACHE_COMPETITIONSETTINGS = "Competition:{compid}:Settings";
    private const string __CACHE_COMPETITIONSUMMARY = "Competition:{compid}:Summary";
    private const string __CACHE_COMPETITIONSIGNUPSETTINGS = "Competition:{compid}:SignupSettings";

    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<GetCompetitionSettingsByCompetitionIdHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    private readonly IReportParser<CompetitionSettingsDto> _competitionSettingsReportParser = competitionSettingsReportParser ?? throw new ArgumentNullException(nameof(competitionSettingsReportParser));
    private readonly IReportParser<CompetitionSummaryDto> _competitionSummaryReportParser = competitionSummaryReportParser ?? throw new ArgumentNullException(nameof(competitionSummaryReportParser));
    private readonly IReportParser<CompetitionSignupSettingsDto> _competitionSignupSettingsReportParser = competitionSignupSettingsReportParser ?? throw new ArgumentNullException(nameof(competitionSignupSettingsReportParser));

    public async override Task<CompetitionSettingsDto?> Handle(GetCompetitionSettingsByCompetitionIdQuery request, CancellationToken cancellationToken)
    {
        var competitionSettingsUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.CompetitionSettingsUrl}".Replace("{compid}", request.CompetitionId);
        var competitionSummaryUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.CompetitionSummaryUrl}".Replace("{compid}", request.CompetitionId);
        var competitionSignupSettingsUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.CompetitionSignupSettingsUrl}".Replace("{compid}", request.CompetitionId);

        var settingsTask = _dataProvider.GetData<CompetitionSettingsDto>(competitionSettingsUrl, _competitionSettingsReportParser, __CACHE_COMPETITIONSETTINGS.Replace("{compid}", request.CompetitionId), TimeSpan.FromMinutes(_settings.Cache.Default_TTL_Mins));
        var summaryTask = _dataProvider.GetData<CompetitionSummaryDto>(competitionSummaryUrl, _competitionSummaryReportParser, __CACHE_COMPETITIONSUMMARY.Replace("{compid}", request.CompetitionId), TimeSpan.FromMinutes(_settings.Cache.Default_TTL_Mins));
        var signupTask = _dataProvider.GetData<CompetitionSignupSettingsDto>(competitionSignupSettingsUrl, _competitionSignupSettingsReportParser, __CACHE_COMPETITIONSIGNUPSETTINGS.Replace("{compid}", request.CompetitionId), TimeSpan.FromMinutes(_settings.Cache.Default_TTL_Mins));

        await Task.WhenAll(settingsTask, summaryTask, signupTask);

        var competitionSettings = settingsTask.Result;
        var competitionSummary = summaryTask.Result;
        var competitionSignupSettings = signupTask.Result;

        if (competitionSettings != null && competitionSettings.Any())
        {
            _logger.LogInformation($"Successfully retrieved the competition settings or {request.CompetitionId}.");

            var retVal = competitionSettings.FirstOrDefault();
            if (retVal != null)
            {
                retVal.MultiPartCompetition = competitionSummary?.FirstOrDefault()?.MultiPartCompetition;
                retVal.SignupSettings = competitionSignupSettings?.FirstOrDefault();
            }

            return retVal;
        }

        return null;
    }
}
