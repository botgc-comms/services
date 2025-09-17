using BOTGC.API.Dto;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetCompetitionResultsForLadiesHandler(IOptions<AppSettings> settings,
                                                       IMediator mediator,
                                                       ILogger<GetCompetitionResultsForLadiesHandler> logger) : QueryHandlerBase<GetCompetitionResultsForLadiesQuery, List<PlayerCompetitionResultsDto>>
    {
        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly ILogger<GetCompetitionResultsForLadiesHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async override Task<List<PlayerCompetitionResultsDto>> Handle(GetCompetitionResultsForLadiesQuery request, CancellationToken cancellationToken)
        {
            var ladyMembersQuery = new GetLadyMembersQuery();
            var ladyMembers = await _mediator.Send(ladyMembersQuery, cancellationToken);

            var semaphore = new SemaphoreSlim(_settings.ConcurrentRequestThrottle);

            var ladyMemberQueries = ladyMembers
                .Where(m => m.MemberNumber.HasValue && m.MemberNumber != 0)
                .Select(m => new GetCompetitionResultsByMemberIdQuery
                {
                    MemberId = m.MemberNumber.ToString()!,
                    FromDate = request.FromDate,
                    ToDate = request.ToDate,
                    MaxFinishingPosition = request.MaxFinishingPosition
                }).ToList();

            var ladyMemberTasks = ladyMemberQueries.Select(async query =>
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

            var results = (await Task.WhenAll(ladyMemberTasks)).Where(r => r.CompetitionResults.Count != 0).ToList();

            _logger.LogInformation($"Successfully retrieved competition results for {results.Count} ladies.");

            return results.ToList();
        }
    }
}
