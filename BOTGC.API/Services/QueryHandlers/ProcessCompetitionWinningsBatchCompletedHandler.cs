using BOTGC.API.Services.Queries;
using MediatR;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class ProcessCompetitionWinningsBatchCompletedHandler(
        ILogger<ProcessCompetitionWinningsBatchCompletedHandler> logger,
        IMediator mediator
    ) : QueryHandlerBase<ProcessCompetitionWinningsBatchCompletedCommand, bool>
{
    private readonly ILogger<ProcessCompetitionWinningsBatchCompletedHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    public override async Task<bool> Handle(ProcessCompetitionWinningsBatchCompletedCommand request, CancellationToken cancellationToken)
    {
        if (request.CompetitionIds is null || request.CompetitionIds.Count == 0)
        {
            _logger.LogInformation("No competition IDs supplied in batch; skipping.");
            return true;
        }

        var ids = request.CompetitionIds.Distinct().OrderBy(x => x).ToList();

        _logger.LogInformation(
            "Processing winnings batch completion for CompetitionIds: {Ids}.",
            string.Join(", ", ids));

        // 1) Generate/refresh child winners pages for each competition
        foreach (var competitionId in ids)
        {
            await _mediator.Send(
                new GenerateCompetitionWinnersHtmlPageCommand(competitionId),
                cancellationToken);
        }

        // 2) Regenerate the parent competition results page once
        await _mediator.Send(new UpdateCompetitionResultsPageCommand(), cancellationToken);

        _logger.LogInformation(
            "Completed winners pages and parent results page update for CompetitionIds: {Ids}.",
            string.Join(", ", ids));

        return true;
    }
}