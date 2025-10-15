using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class CreateStockTakeMondayTicketsHandler(
    IOptions<AppSettings> settings,
    ILogger<CreateStockTakeMondayTicketsHandler> logger,
    ITaskBoardService taskBoardService
) : QueryHandlerBase<CreateStockTakeMondayTicketsCommand, bool>
{
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<CreateStockTakeMondayTicketsHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ITaskBoardService _taskBoardService = taskBoardService ?? throw new ArgumentNullException(nameof(taskBoardService));

    public override async Task<bool> Handle(CreateStockTakeMondayTicketsCommand request, CancellationToken cancellationToken)
    {
        var t = request.Ticket;

        _logger.LogInformation(
            "StockTake->Monday: creating tickets for {Date} / {Division}. Accepted={Accepted} Investigate={Investigate}.",
            t.Date.ToString("yyyy-MM-dd"),
            t.Division,
            t.AcceptedItems.Count,
            t.InvestigateItems.Count
        );

        string? igLink = ""; // optional; set in AppSettings if you have it, else stays null

        var parentId = await _taskBoardService.CreateStockTakeAndInvestigationsAsync(t, igLink);

        _logger.LogInformation("StockTake->Monday: created parent item {ParentItemId} for {Date} / {Division}.", parentId, t.Date.ToString("yyyy-MM-dd"), t.Division);

        return true;
    }
}


