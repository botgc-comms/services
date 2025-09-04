using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetCompeitionResultsForJuniorsHandler(IOptions<AppSettings> settings,
                                                       IMediator mediator,
                                                       ILogger<GetCompeitionResultsForJuniorsHandler> logger, 
                                                       IServiceProvider serviceProvider) : QueryHandlerBase<GetCompetitionResultsForJuniorsQuery, List<PlayerCompetitionResultsDto>>
    {
        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly ILogger<GetCompeitionResultsForJuniorsHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async override Task<List<PlayerCompetitionResultsDto>> Handle(GetCompetitionResultsForJuniorsQuery request, CancellationToken cancellationToken)
        {
            var juniorMembersQuery = new GetJuniorMembersQuery();
            var juniorMembers = await _mediator.Send(juniorMembersQuery, cancellationToken);

            var semaphore = new SemaphoreSlim(_settings.ConcurrentRequestThrottle); 

            var juniorMemberQueries = juniorMembers
                .Where(j => j.MemberNumber.HasValue && j.MemberNumber != 0)
                .Select(j => new GetCompetitionResultsByMemberIdQuery
            {
                MemberId = j.MemberNumber.ToString()!,
                FromDate = request.FromDate,
                ToDate = request.ToDate, 
                MaxFinishingPosition = request.MaxFinishingPosition
            }).ToList();    

            var juniorMemberTasks = juniorMemberQueries.Select(async query =>
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

            var results = (await Task.WhenAll(juniorMemberTasks)).Where(r => r.CompetitionResults.Count != 0).ToList();

            _logger.LogInformation($"Successfully retrieved competition results for {results.Count} juniuors.");

            return results.ToList();
        }
    }
}
