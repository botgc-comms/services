using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public class GetCompetitionLeaderboardByCompetitionHandler(IOptions<AppSettings> settings,
                                                           IMediator mediator,
                                                           ILogger<GetCompetitionLeaderboardByCompetitionHandler> logger,
                                                           IDataProvider dataProvider,
                                                           IReportParserWithMetadata<LeaderBoardDto, CompetitionSettingsDto> reportParser) : QueryHandlerBase<GetCompetitionLeaderboardByCompetitionQuery, LeaderBoardDto?>
{
    private const string __CACHE_KEY = "Leaderboard_Settings:{compid}";

    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILogger<GetCompetitionLeaderboardByCompetitionHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    private readonly IReportParserWithMetadata<LeaderBoardDto, CompetitionSettingsDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

    public async override Task<LeaderBoardDto?> Handle(GetCompetitionLeaderboardByCompetitionQuery request, CancellationToken cancellationToken)
    {
        var competitionId = request.CompetitionId ?? throw new ArgumentNullException(nameof(request.CompetitionId), "Competition ID cannot be null.");

        var competitionSettingsQuery = new GetCompetitionSettingsByCompetitionIdQuery { CompetitionId = competitionId };
        var competitionSettings = await _mediator.Send(competitionSettingsQuery, cancellationToken);

        TimeSpan cacheTTL;
        var today = DateTime.Today;
        var compDate = competitionSettings?.Date.Date ?? today;

        if (compDate == today)
        {
            cacheTTL = TimeSpan.FromSeconds(15);
        }
        else if (compDate >= today.AddDays(-7) && compDate < today)
        {
            cacheTTL = TimeSpan.FromHours(2);
        }
        else
        {
            cacheTTL = TimeSpan.FromMinutes(_settings.Cache.Forever_TTL_Mins);
        }

        var grossOrNett = competitionSettings!.ResultsDisplay.ToLower().Contains("net") ? "1" : "2";

        var allUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.LeaderBoardUrl}"
            .Replace("{compid}", competitionId)
            .Replace("{grossOrNett}", grossOrNett)
            .Replace("{division}", "all");

        var allList = await _dataProvider.GetData<LeaderBoardDto, CompetitionSettingsDto>(
            allUrl,
            _reportParser,
            competitionSettings,
            __CACHE_KEY.Replace("{compid}", competitionId),
            cacheTTL);

        LeaderBoardDto retVal = allList?.FirstOrDefault() ?? new LeaderBoardDto
        {
            Players = new List<LeaderboardPlayerDto>(),
            CompetitionDetails = competitionSettings,
            Divisions = new List<DivisionDto>()
        };

        if (retVal.CompetitionDetails == null) retVal.CompetitionDetails = competitionSettings;
        if (retVal.Players == null) retVal.Players = new List<LeaderboardPlayerDto>();

        var divisionCount = Math.Clamp(competitionSettings.Divisions ?? 0, 0, 4);

        if (divisionCount > 0)
        {
            var tasks = new List<Task<(int Division, LeaderBoardDto? Dto)>>();

            for (int d = 1; d <= divisionCount; d++)
            {
                var divUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.LeaderBoardUrl}"
                    .Replace("{compid}", competitionId)
                    .Replace("{grossOrNett}", grossOrNett)
                    .Replace("{division}", d.ToString());

                var cacheKey = __CACHE_KEY.Replace("{compid}", competitionId) + $":Division_{d}";

                tasks.Add(GetDivisionAsync(d, divUrl, competitionSettings, cacheKey, cacheTTL));
            }

            var results = await Task.WhenAll(tasks);

            var divisions = new List<DivisionDto>();
            foreach (var (Division, Dto) in results)
            {
                var players = Dto?.Players ?? new List<LeaderboardPlayerDto>();
                divisions.Add(new DivisionDto
                {
                    Number = Division,
                    Limit = GetDivisionLimit(competitionSettings, Division),
                    Players = players
                });
            }

            retVal.Divisions = divisions;
        }
        else
        {
            retVal.Divisions = new List<DivisionDto>();
        }

        _logger.LogInformation("Retrieved leaderboard for competition {CompetitionId}. Players: {PlayersCount}. Divisions: {DivisionsCount}.",
            competitionId, retVal.Players.Count, retVal.Divisions.Count);

        return retVal;
    }

    private async Task<(int Division, LeaderBoardDto? Dto)> GetDivisionAsync(int division,
                                                                             string url,
                                                                             CompetitionSettingsDto settings,
                                                                             string cacheKey,
                                                                             TimeSpan ttl)
    {
        var list = await _dataProvider.GetData<LeaderBoardDto, CompetitionSettingsDto>(url, _reportParser, settings, cacheKey, ttl);
        return (division, list?.FirstOrDefault());
    }

    private static int? GetDivisionLimit(CompetitionSettingsDto settings, int division) =>
        division switch
        {
            1 => settings.Division1Limit,
            2 => settings.Division2Limit,
            3 => settings.Division3Limit,
            4 => settings.Division4Limit,
            _ => null
        };
}
