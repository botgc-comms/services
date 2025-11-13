using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class GenerateCompetitionPrizeInvoicePdfHandler(
        ICompetitionPrizeInvoicePdfGeneratorService pdfGenerator,
        ILogger<GenerateCompetitionPrizeInvoicePdfHandler> logger
    ) : QueryHandlerBase<GenerateCompetitionPrizeInvoicePdfCommand, byte[]>
{
    private readonly ICompetitionPrizeInvoicePdfGeneratorService _pdfGenerator = pdfGenerator ?? throw new ArgumentNullException(nameof(pdfGenerator));
    private readonly ILogger<GenerateCompetitionPrizeInvoicePdfHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async override Task<byte[]> Handle(GenerateCompetitionPrizeInvoicePdfCommand request, CancellationToken cancellationToken)
    {
        if (request.Summary == null)
        {
            throw new ArgumentNullException(nameof(request.Summary));
        }

        _logger.LogInformation(
            "Handling GenerateCompetitionPrizeInvoicePdfCommand for CompetitionId {CompetitionId}, InvoiceId {InvoiceId}.",
            request.Summary.CompetitionId,
            request.InvoiceId);

        var pdfBytes = _pdfGenerator.GenerateInvoice(request.Summary, request.InvoiceId);

        if (pdfBytes == null || pdfBytes.Length == 0)
        {
            throw new InvalidOperationException(
                $"Failed to generate invoice PDF for CompetitionId {request.Summary.CompetitionId}, InvoiceId {request.InvoiceId}.");
        }

        return await Task.FromResult(pdfBytes);
    }
}