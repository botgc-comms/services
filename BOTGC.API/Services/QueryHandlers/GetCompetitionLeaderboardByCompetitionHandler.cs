using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetCompetitionLeaderboardByCompetitionHandler(IOptions<AppSettings> settings,
                                                               IMediator mediator,
                                                               ILogger<GetCompetitionLeaderboardByCompetitionHandler> logger,
                                                               IDataProvider dataProvider,
                                                               IReportParserWithMetadata<LeaderBoardDto, CompetitionSettingsDto> reportParser) : QueryHandlerBase<GetCompetitionLeaderboardByCompetitionQuery, LeaderBoardDto?>
    {
        private const string __CACHE_KEY = "Leaderboard_Settings_{compid}";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly ILogger<GetCompetitionLeaderboardByCompetitionHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParserWithMetadata<LeaderBoardDto, CompetitionSettingsDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

        public async override Task<LeaderBoardDto?> Handle(GetCompetitionLeaderboardByCompetitionQuery request, CancellationToken cancellationToken)
        {
            var competitionId = request.CompetitionId ?? throw new ArgumentNullException(nameof(request.CompetitionId), "Competition ID cannot be null.");
            
            var competitionSettingsQuery = new GetCompetitionSettingsByCompetitionIdQuery
            {
                CompetitionId = competitionId
            };

            var competitionSettings = await _mediator.Send(competitionSettingsQuery, cancellationToken);

            var grossOrNett = competitionSettings!.ResultsDisplay.ToLower().Contains("net") ? "1" : "2";
            var leaderboardUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.LeaderBoardUrl}".Replace("{compid}", competitionId).Replace("{grossOrNett}", grossOrNett);
            var leaderboard = await _dataProvider.GetData<LeaderBoardDto, CompetitionSettingsDto>(leaderboardUrl, _reportParser, competitionSettings, __CACHE_KEY.Replace("{compid}", competitionId), TimeSpan.FromMinutes(_settings.Cache.VeryShortTerm_TTL_mins));

            if (leaderboard != null && leaderboard.Any())
            {
                _logger.LogInformation($"Successfully retrieved the leaderboard for competition {competitionId}.");

                var retVal = leaderboard.FirstOrDefault();
                if (retVal != null) retVal.CompetitionDetails = competitionSettings;

                return retVal;
            }

            return null;
        }
    }
}
