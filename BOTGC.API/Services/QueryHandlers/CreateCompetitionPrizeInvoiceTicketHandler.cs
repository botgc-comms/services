using BOTGC.API.Common;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class CreateCompetitionPrizeInvoiceTicketHandler(
        ILogger<CreateCompetitionPrizeInvoiceTicketHandler> logger,
        ITaskBoardService taskBoardService)
        : QueryHandlerBase<CreateCompetitionPrizeInvoiceTicketCommand, string?>
{
    private readonly ILogger<CreateCompetitionPrizeInvoiceTicketHandler> _logger =
        logger ?? throw new ArgumentNullException(nameof(logger));

    private readonly ITaskBoardService _taskBoardService =
        taskBoardService ?? throw new ArgumentNullException(nameof(taskBoardService));

    public override async Task<string?> Handle(CreateCompetitionPrizeInvoiceTicketCommand request, CancellationToken cancellationToken)
    {
        if (request.Summary is null)
            throw new ArgumentNullException(nameof(request.Summary));

        if (request.PdfBytes is null || request.PdfBytes.Length == 0)
            throw new ArgumentException("PDF bytes are required.", nameof(request.PdfBytes));

        var summary = request.Summary;

        // Group name can be static or per-year; tune as you like
        var groupTitle = "Finance";

        var dateText = summary.CompetitionDate.ToString("dd MMM yyyy");
        var itemName = $"Invoice {request.InvoiceId} — {summary.CompetitionName} ({dateText})";

        _logger.LogInformation(
            "Creating Monday finance task for prize invoice {InvoiceId} (CompetitionId {CompetitionId}).",
            request.InvoiceId,
            summary.CompetitionId);

        var nextWorkingDay = DateHelper.NextWorkingDay(DateTime.Now);

        // Create task on Stacy board
        var itemId = await _taskBoardService.CreateFinanceTaskAsync(
            groupTitle: groupTitle,
            taskName: itemName,
            assigneeEmail: null,         
            statusLabel: "To do",
            deadline: nextWorkingDay
        );

        if (string.IsNullOrWhiteSpace(itemId))
        {
            _logger.LogWarning(
                "Failed to create Monday finance task for prize invoice {InvoiceId} (CompetitionId {CompetitionId}).",
                request.InvoiceId,
                summary.CompetitionId);

            return null;
        }

        var fileName = $"Prize-Invoice-{request.InvoiceId}.pdf";

        // Attach the invoice PDF to the Files column on Stacy’s board
        await _taskBoardService.AttachFinanceInvoiceFileAsync(itemId, request.PdfBytes, fileName);

        _logger.LogInformation(
            "Created Monday finance task {ItemId} and attached invoice {InvoiceId} for CompetitionId {CompetitionId}.",
            itemId,
            request.InvoiceId,
            summary.CompetitionId);

        return itemId;
    }
}