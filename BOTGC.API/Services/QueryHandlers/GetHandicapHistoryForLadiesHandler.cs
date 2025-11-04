using BOTGC.API.Dto;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public class GetHandicapHistoryForLadiesHandler(IOptions<AppSettings> settings,
                                                IMediator mediator,
                                                ILogger<GetHandicapHistoryForLadiesHandler> logger) : QueryHandlerBase<GetHandicapSummaryForLadiesQuery, List<PlayerHandicapSummaryDto>>
{
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILogger<GetHandicapHistoryForLadiesHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async override Task<List<PlayerHandicapSummaryDto>> Handle(GetHandicapSummaryForLadiesQuery request, CancellationToken cancellationToken)
    {
        var ladyMembersQuery = new GetLadyMembersQuery();
        var ladyMembers = await _mediator.Send(ladyMembersQuery, cancellationToken);

        var semaphore = new SemaphoreSlim(_settings.ConcurrentRequestThrottle);

        var ladyMemberQueries = ladyMembers
            .Where(m => m.MemberNumber.HasValue && m.MemberNumber != 0)
            .Select(m => new GetHandicapHistoryByMemberQuery
            {
                MemberId = m.MemberNumber!.Value
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

        var results = (await Task.WhenAll(ladyMemberTasks))
            .Where(r => r != null && r.HandicapIndexPoints.Count != 0)
            .ToList();

        _logger.LogInformation($"Successfully retrieved handicap history results for {results.Count} ladies.");

        return results.ToList();
    }
}
