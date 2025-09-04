using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetCompetitionResultsByMemberIdHandler(IOptions<AppSettings> settings,
                                                        IMediator mediator,
                                                        ILogger<GetCompetitionResultsByMemberIdHandler> logger, 
                                                        IServiceProvider serviceProvider) : QueryHandlerBase<GetCompetitionResultsByMemberIdQuery, PlayerCompetitionResultsDto>
    {
        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly ILogger<GetCompetitionResultsByMemberIdHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async override Task<PlayerCompetitionResultsDto> Handle(GetCompetitionResultsByMemberIdQuery request, CancellationToken cancellationToken)
        {
            PlayerCompetitionResultsDto result = null;

            var playerDetailsQuery = new GetPlayerIdsByMemberQuery();
            var playerDetails = await _mediator.Send(playerDetailsQuery, cancellationToken);

            try
            {

                var playerRoundsQuery = new GetRoundsByMemberIdQuery
                {
                    MemberId = request.MemberId,
                    FromDate = request.FromDate,
                    ToDate = request.ToDate
                };

                var playerRounds = await _mediator.Send(playerRoundsQuery, cancellationToken);

                var playerCompetitionQueries = playerRounds
                    .Where(r => !r.IsGeneralPlay && r.CompetitionId.HasValue).Select(r => r.CompetitionId.Value)
                    .Distinct()
                    .Select(r => new GetCompetitionLeaderboardByCompetitionQuery
                    {
                        CompetitionId = r.ToString()
                    })
                    .ToList();

                var semaphore = new SemaphoreSlim(_settings.ConcurrentRequestThrottle);

                var leaderboardTasks = playerCompetitionQueries.Select(async query =>
                {
                    await semaphore.WaitAsync(cancellationToken);
                    try
                    {
                        return await _mediator.Send(query, cancellationToken);
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }).ToList();

                var leaderboards = await Task.WhenAll(leaderboardTasks);

                var player = playerDetails.Where(p => p.MemberId.ToString() == request.MemberId.ToString()).FirstOrDefault();

                result = new PlayerCompetitionResultsDto
                {
                    MemberId = request.MemberId,
                    PlayerId = player?.PlayerId,
                    PlayerName = $"{player.Forename ?? ""} {player.Surname ?? ""}".Trim(),
                    CompetitionResults = leaderboards
                        .Where(lb => lb != null)
                        .Select(lb => new PlayerCompetitionResultDto
                        {
                            CompetitionDetails = lb!.CompetitionDetails,
                            LeaderBoardEntry = lb!.Players.FirstOrDefault(p => p.PlayerId == playerDetails.Where(pd => pd.MemberId.ToString() == request.MemberId.ToString()).FirstOrDefault()?.PlayerId),
                        })
                        .Where(r => r.LeaderBoardEntry != null && r.LeaderBoardEntry.Position <= request.MaxFinishingPosition.GetValueOrDefault(int.MaxValue))
                        .OrderBy(cr => cr.CompetitionDetails?.Date)
                        .ToList()
                };

                _logger.LogInformation($"Successfully retrieved {result.CompetitionResults?.Count} competition results for player {result.MemberId}.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            return result;
        }
    }
}
