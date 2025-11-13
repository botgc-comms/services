using System.Text;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class GenerateCompetitionWinnersHtmlPageHandler(
        IBlobStorageService blobStorage,
        ILogger<GenerateCompetitionWinnersHtmlPageHandler> logger,
        IMediator mediator)
    : QueryHandlerBase<GenerateCompetitionWinnersHtmlPageCommand, string?>
{
    private readonly IBlobStorageService _blobStorage = blobStorage ?? throw new ArgumentNullException(nameof(blobStorage));
    private readonly ILogger<GenerateCompetitionWinnersHtmlPageHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    public override async Task<string?> Handle(GenerateCompetitionWinnersHtmlPageCommand request, CancellationToken cancellationToken)
    {
        if (request.CompetitionId <= 0)
        {
            throw new ArgumentException("CompetitionId must be a positive integer.", nameof(request.CompetitionId));
        }

        _logger.LogInformation(
            "Starting prize invoice workflow for CompetitionId {CompetitionId}.",
            request.CompetitionId);

        // Load the full payout details (canonical source for invoice + downstream steps).
        var summary = await _mediator.Send(
            new GetCompetitionPayoutDetailsQuery(request.CompetitionId),
            cancellationToken);

        if (summary == null)
        {
            _logger.LogError(
                "No payout details found for CompetitionId {CompetitionId}. Aborting invoice workflow.",
                request.CompetitionId);

            throw new ApplicationException(
                $"Payout details not found for CompetitionId {request.CompetitionId}.");
        }

        // Build deterministic blob name
        var blobName = CompetitionWinnersBlobNaming.GetBlobName(summary);

        // Generate HTML
        var html = CompetitionWinnersHtmlBuilder.BuildWinnersPageHtml(summary);
        var bytes = Encoding.UTF8.GetBytes(html);

        _logger.LogInformation(
            "Uploading winners HTML page for CompetitionId {CompetitionId} to blob {BlobName}.",
            summary.CompetitionId,
            blobName);

        var url = await _blobStorage.UploadAsync(
            CompetitionWinnersBlobNaming.ContainerName,
            blobName,
            bytes,
            contentType: "text/html",
            cancellationToken: cancellationToken);

        _logger.LogInformation(
            "Uploaded winners HTML page for CompetitionId {CompetitionId} to {Url}.",
            summary.CompetitionId,
            url);

        return url;
    }
}
