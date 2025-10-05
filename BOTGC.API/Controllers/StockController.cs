using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Controllers
{
    [ApiController]
    [Route("api/stock")]
    [Produces("application/json")]
    public class StockController(
        IOptions<AppSettings> settings,
        ILogger<StockController> logger,
        IMediator mediator,
        IStockAnalysisTaskQueue taskQueue,
        IServiceScopeFactory serviceScopeFactory) : Controller
    {
        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        private readonly ILogger<StockController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IStockAnalysisTaskQueue _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));

        [HttpGet("stockLevels")]
        public async Task<IActionResult> GetStockLevels()
        {
            _logger.LogInformation($"Fetching current stock levels...");

            try
            {
                var query = new GetStockLevelsQuery();
                
                var stockItems = await _mediator.Send(query, HttpContext.RequestAborted);

                if (stockItems == null || stockItems.Count == 0)
                {
                    _logger.LogWarning($"No stock items where found.");
                    return NoContent();
                }

                _logger.LogInformation($"Successfully retrieved {stockItems.Count} stock items.");
                return Ok(stockItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving stock items.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while retrieving stock items.");
            }
        }

        [HttpGet("tillOperators")]
        public async Task<IActionResult> GetTillOperators()
        {
            _logger.LogInformation($"Fetching current till operators...");

            try
            {
                var query = new GetTillOperatorsQuery();

                var tillOperators = await _mediator.Send(query, HttpContext.RequestAborted);

                if (tillOperators == null || tillOperators.Count == 0)
                {
                    _logger.LogWarning($"No stock items where found.");
                    return NoContent();
                }

                _logger.LogInformation($"Successfully retrieved {tillOperators.Count} till operators.");
                return Ok(tillOperators);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving till operators.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while retrieving till operators.");
            }
        }

        [HttpGet("wasteSheet")]
        public async Task<IActionResult> GetWasteSheet([FromQuery] DateTime? day = null)
        {
            var sheetDate = (day?.Date ?? DateTime.UtcNow.Date);
            _logger.LogInformation("Fetching waste sheet for {SheetDate}...", sheetDate.ToString("yyyy-MM-dd"));

            try
            {
                var query = new GetWasteSheetQuery(sheetDate);
                var sheet = await _mediator.Send(query, HttpContext.RequestAborted);

                if (sheet is null || sheet.Entries.Count == 0)
                {
                    _logger.LogInformation("No entries on waste sheet for {SheetDate}.", sheetDate.ToString("yyyy-MM-dd"));
                    return Ok(new WasteSheetDto(sheetDate, "Open", new List<WasteEntryDto>()));
                }

                return Ok(sheet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving waste sheet for {SheetDate}.", sheetDate.ToString("yyyy-MM-dd"));
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the waste sheet.");
            }
        }


        [HttpPost("wasteSheet/entry")]
        public async Task<IActionResult> AddToWasteSheet([FromBody] AddWasteEntryRequest request, [FromQuery] DateTime? day = null)
        {
            var sheetDate = (day?.Date ?? DateTime.UtcNow.Date);
            _logger.LogInformation("Adding waste entry to sheet {SheetDate} for product {ProductName}.", sheetDate.ToString("yyyy-MM-dd"), request.ProductName);

            try
            {
                var cmd = new AddToWasteSheetCommand(
                    sheetDate,
                    request.ClientEntryId,
                    request.OperatorId,
                    request.ProductId,
                    request.IGProductId,
                    request.Unit,
                    request.ProductName,
                    request.Reason,
                    request.Quantity,
                    request.DeviceId
                );

                var result = await _mediator.Send(cmd, HttpContext.RequestAborted);

                if (result.Duplicated)
                {
                    // Idempotent success (already processed)
                    return Ok(new { ok = true, duplicated = true });
                }

                return Created($"/api/stock/wasteSheet?day={sheetDate:yyyy-MM-dd}", new { ok = true, duplicated = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding waste entry for {SheetDate}.", sheetDate.ToString("yyyy-MM-dd"));
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while adding the waste entry.");
            }
        }

        [HttpDelete("wasteSheet/entry/{id}")]
        public async Task<IActionResult> DeleteFromWasteSheet(Guid id, [FromQuery] DateTime? day = null)
        {
            var sheetDate = (day?.Date ?? DateTime.UtcNow.Date);
            _logger.LogInformation("Deleting waste entry {EntryId} from sheet {SheetDate}.", id, sheetDate.ToString("yyyy-MM-dd"));

            try
            {
                var cmd = new DeleteFromWasteSheetCommand(sheetDate, id);
                var result = await _mediator.Send(cmd, HttpContext.RequestAborted);

                if (!result.Found)
                {
                    return NotFound(new { ok = false, message = "Entry not found" });
                }

                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting waste entry {EntryId} for {SheetDate}.", id, sheetDate.ToString("yyyy-MM-dd"));
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the waste entry.");
            }
        }

        [HttpPost("analyse")]
        public async Task<IActionResult> QueueStockAnalysis([FromBody] StockAnalysisTaskItem request)
        {
            _logger.LogInformation("Received request to queue stock analysis.");

            if (request == null)
            {
                _logger.LogWarning("No task data supplied in the request body.");
                return BadRequest("Missing task data.");
            }

            await _taskQueue.QueueTaskAsync(request);

            return Accepted(new { Message = "Stock analysis task queued." });
        }
    }
}
