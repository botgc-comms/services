using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetHandicapHistoryForJuniorsHandler(IOptions<AppSettings> settings,
                                                     IMediator mediator,
                                                     ILogger<GetHandicapHistoryForJuniorsHandler> logger) : QueryHandlerBase<GetHandicapSummaryForJuniorsQuery, List<PlayerHandicapSummaryDto>>
    {
        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly ILogger<GetHandicapHistoryForJuniorsHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async override Task<List<PlayerHandicapSummaryDto>> Handle(GetHandicapSummaryForJuniorsQuery request, CancellationToken cancellationToken)
        {
            var juniorMembersQuery = new GetJuniorMembersQuery();
            var juniorMembers = await _mediator.Send(juniorMembersQuery, cancellationToken);

            var semaphore = new SemaphoreSlim(_settings.ConcurrentRequestThrottle); 

            var juniorMemberQueries = juniorMembers
                .Where(j => j.MemberNumber.HasValue && j.MemberNumber != 0)
                .Select(j => new GetHandicapHistoryByMemberQuery
            {
                MemberId = j.MemberNumber!.Value
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

            var results = (await Task.WhenAll(juniorMemberTasks)).Where(r => r != null && r.HandicapIndexPoints.Count != 0).ToList();

            _logger.LogInformation($"Successfully retrieved handicap history results for {results.Count} juniuors.");

            return results.ToList();
        }
    }
}
