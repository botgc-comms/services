using BOTGC.API.Dto;
using BOTGC.API.Services.Queries;
using MediatR;
using System.Globalization;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class ProcessPrizeInvoiceHandler(
    IMediator mediator,
    ILogger<ProcessPrizeInvoiceHandler> logger
) : QueryHandlerBase<ProcessPrizeInvoiceCommand, bool>
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILogger<ProcessPrizeInvoiceHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async override Task<bool> Handle(ProcessPrizeInvoiceCommand request, CancellationToken cancellationToken)
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

        var anyPlacings = summary.Divisions.SelectMany(d => d.Placings).Any();
        if (!anyPlacings)
        {
            _logger.LogWarning(
                "No placings found for CompetitionId {CompetitionId}. Invoice will not be generated.",
                request.CompetitionId);
            return true;
        }

        // Generate a deterministic invoice id so retries are idempotent.
        var invoiceId = BuildInvoiceId(summary);

        // 1. Generate the PDF invoice
        var pdfBytes = await _mediator.Send(
            new GenerateCompetitionPrizeInvoicePdfCommand(summary, invoiceId),
            cancellationToken);

        if (pdfBytes == null || pdfBytes.Length == 0)
        {
            _logger.LogError(
                "PDF generation failed for CompetitionId {CompetitionId}, InvoiceId {InvoiceId}.",
                request.CompetitionId,
                invoiceId);

            throw new ApplicationException(
                $"PDF generation failed for CompetitionId {request.CompetitionId}, InvoiceId {invoiceId}.");    
        }

        // 2. Upload the invoice to storage
        var invoiceUrl = await _mediator.Send(
            new UploadCompetitionPrizeInvoiceCommand(summary, invoiceId, pdfBytes),
            cancellationToken);

        // 3. Create the Monday.com ticket (or equivalent)
        var ticketId = await _mediator.Send(
            new CreateCompetitionPrizeInvoiceTicketCommand(summary, pdfBytes, invoiceId),
            cancellationToken);

        // 4. Send the email to the pro shop
        var emailOk = await _mediator.Send(
            new SendCompetitionPrizeInvoiceEmailCommand(summary, pdfBytes, ticketId, invoiceId, invoiceUrl),
            cancellationToken);

        if (!emailOk)
        {
            _logger.LogError(
                "Pro shop invoice email failed for CompetitionId {CompetitionId}, InvoiceId {InvoiceId}.",
                request.CompetitionId,
                invoiceId);

            throw new ApplicationException(
                $"Pro shop invoice email failed for CompetitionId {request.CompetitionId}, InvoiceId {invoiceId}.");    
        }

        _logger.LogInformation(
            "Successfully completed prize invoice workflow for CompetitionId {CompetitionId}, InvoiceId {InvoiceId}.",
            request.CompetitionId,
            invoiceId);

        return true;
    }

    private static string BuildInvoiceId(CompetitionWinningsSummaryDto summary)
    {
        var date = summary.CompetitionDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(summary.CompetitionDate, DateTimeKind.Utc)
            : summary.CompetitionDate.ToUniversalTime();

        // Deterministic per competition: safe for retries, unique per competition.
        return $"INV-{summary.CompetitionId}-{date.ToString("yyyyMMdd", CultureInfo.InvariantCulture)}";
    }
}
