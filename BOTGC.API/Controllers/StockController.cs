using BOTGC.API.Dto;
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

        [HttpGet("stockItems")]
        public async Task<IActionResult> GetStockItems()
        {
            _logger.LogInformation($"Fetching current stock items...");

            try
            {
                var query = new GetStockItemsAndTradeUnitsQuery();

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
                    request.Quantity
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

        [HttpPut("stockItems/{id}")]
        public async Task<IActionResult> UpdateStockItem([FromRoute] string id, [FromBody] UpdateStockItemCommand cmd)
        {
            using var _ = _logger.BeginScope(new Dictionary<string, object?>
            {
                ["RouteId"] = id
            });

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for UpdateStockItem.");
                return ValidationProblem(ModelState);
            }

            if (!int.TryParse(id, out var routeStockId))
            {
                _logger.LogWarning("Route id '{Id}' is not a valid integer.", id);
                return BadRequest(new { message = "Invalid stock item id in route." });
            }

            // Ensure the command has the correct StockId
            if (cmd is null)
            {
                _logger.LogWarning("Request body is null for UpdateStockItem {RouteStockId}.", routeStockId);
                return BadRequest(new { message = "Request body cannot be null." });
            }

            // If the command has no StockId set, populate it from the route; otherwise ensure it matches.
            if (cmd.StockId != routeStockId)
            {
                _logger.LogWarning("Route id {RouteStockId} does not match body StockId {BodyStockId}.", routeStockId, cmd.StockId);
                return BadRequest(new { message = "Route id does not match body StockId." });
            }

            try
            {
                _logger.LogInformation("Updating stock item {StockId}.", routeStockId);

                var updated = await _mediator.Send(cmd, HttpContext.RequestAborted);
                if (!updated)
                {
                    _logger.LogError("Failed to update stock item {StockId}.", routeStockId);
                    return StatusCode(StatusCodes.Status502BadGateway, new { ok = false, message = "Upstream update failed." });
                }

                _logger.LogInformation("Successfully updated stock item {StockId}.", routeStockId);

                // Return a conventional 200 with a small payload; alternatively NoContent() is fine if you prefer 204.
                return Ok(new { ok = true, id = routeStockId });
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Request cancelled while updating stock item {StockId}.", routeStockId);
                return StatusCode(StatusCodes.Status499ClientClosedRequest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error updating stock item {StockId}.", routeStockId);
                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An error occurred while updating the stock item." });
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

        [HttpGet("stockTakes")]
        public async Task<IActionResult> GetStockTakes([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            _logger.LogInformation("Fetching previous stock takes...");

            try
            {
                var query = new GetStockTakesQuery()
                {
                    FromDate = fromDate,
                    ToDate = toDate   
                };

                var products = await _mediator.Send(query, HttpContext.RequestAborted);

                if (products == null || products.Count == 0)
                {
                    _logger.LogInformation("No previous stock takes were found");
                    return Ok(new List<StockTakeReportEntryDto>());
                }

                _logger.LogInformation($"Successfully retrieved {products.Count} products involved in previous stock takes.");
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, @"Error retrieving previous stock takes.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving previous stock takes.");
            }
        }

        [HttpGet("stockTakes/products")]
        public async Task<IActionResult> GetStockTakeProducts()
        {
            _logger.LogInformation("Fetching prepared stock take product list...");

            try
            {
                var query = new GetStockTakeProductsQuery();
                var products = await _mediator.Send(query, HttpContext.RequestAborted);

                if (products == null || products.Count == 0)
                {
                    _logger.LogInformation("No products found for the stock take.");
                    return Ok(new List<StockTakeReportEntryDto>());
                }

                _logger.LogInformation($"Successfully retrieved {products.Count} products for the stock take.");
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock take product list.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the stock take product list.");
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

        // GET: /api/stock/stockTakes/sheet?day=yyyy-MM-dd&division=MINERALS
        [HttpGet("stockTakes/sheet")]
        public async Task<IActionResult> GetStockTakeSheet([FromQuery] DateTime? day = null, [FromQuery] string? division = null)
        {
            var sheetDate = (day?.Date ?? DateTime.UtcNow.Date);
            var div = division ?? string.Empty;

            _logger.LogInformation("Fetching stock take sheet for {SheetDate} / {Division}...", sheetDate.ToString("yyyy-MM-dd"), div);

            try
            {
                var query = new GetStockTakeSheetQuery(sheetDate, div);
                var sheet = await _mediator.Send(query, HttpContext.RequestAborted);

                if (sheet is null || sheet.Entries.Count == 0)
                {
                    _logger.LogInformation("No entries on stock take sheet for {SheetDate} / {Division}.", sheetDate.ToString("yyyy-MM-dd"), div);
                    return Ok(new StockTakeSheetDto(sheetDate, div, "Open", new List<StockTakeEntryDto>()));
                }

                return Ok(sheet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock take sheet for {SheetDate} / {Division}.", sheetDate.ToString("yyyy-MM-dd"), div);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the stock take sheet.");
            }
        }

        // POST: /api/stock/stockTakes/sheet/entry?day=yyyy-MM-dd
        [HttpPost("stockTakes/sheet/entry")]
        public async Task<IActionResult> UpsertStockTakeEntry([FromBody] UpsertStockTakeEntryRequest request, [FromQuery] DateTime? day = null)
        {
            var sheetDate = (day?.Date ?? DateTime.UtcNow.Date);

            _logger.LogInformation(
                "Upserting stock take entry for {SheetDate}: {ProductName} ({StockItemId}) in {Division}.",
                sheetDate.ToString("yyyy-MM-dd"), request.Name, request.StockItemId, request.Division
            );

            try
            {
                var cmd = new UpsertStockTakeEntryCommand(
                    sheetDate,
                    request.StockItemId,
                    request.Name,
                    request.Division,
                    request.Unit,
                    request.OperatorId,
                    request.OperatorName,
                    request.At,
                    request.Observations?.Select(o =>
                        new StockTakeObservationDto(o.StockItemId, o.Code, o.Location, o.Value)).ToList() ?? new List<StockTakeObservationDto>(),
                    request.EstimatedQuantityAtCapture
                );

                var result = await _mediator.Send(cmd, HttpContext.RequestAborted);

                return Created($"/api/stock/stockTakes/sheet?day={sheetDate:yyyy-MM-dd}&division={Uri.EscapeDataString(request.Division ?? string.Empty)}",
                               new { ok = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting stock take entry for {SheetDate}.", sheetDate.ToString("yyyy-MM-dd"));
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while saving the stock take entry.");
            }
        }


        [HttpPost("stockTakes/purchaseOrder")]
        public async Task<IActionResult> TestCreatePurchaseOrder([FromBody] PurchaseOrderDto request)
        {
            try
            {
                var cmd = new CreatePurchaseOrderCommand(request);
                var result = await _mediator.Send(cmd, HttpContext.RequestAborted);

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occured creating the purchase order");
            }
        }

        // DELETE: /api/stock/stockTakes/sheet/entry/{stockItemId}?day=yyyy-MM-dd&division=MINERALS
        [HttpDelete("stockTakes/sheet/entry/{stockItemId:int}")]
        public async Task<IActionResult> DeleteStockTakeEntry([FromRoute] int stockItemId, [FromQuery] DateTime? day = null, [FromQuery] string? division = null)
        {
            var sheetDate = (day?.Date ?? DateTime.UtcNow.Date);
            var div = division ?? string.Empty;

            _logger.LogInformation(
                "Deleting stock take entry {StockItemId} from sheet {SheetDate} / {Division}.",
                stockItemId, sheetDate.ToString("yyyy-MM-dd"), div
            );

            try
            {
                var cmd = new DeleteFromStockTakeSheetCommand(sheetDate, div, stockItemId);
                var result = await _mediator.Send(cmd, HttpContext.RequestAborted);

                if (!result.Found)
                {
                    return NotFound(new { ok = false, message = "Entry not found" });
                }

                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting stock take entry {StockItemId} for {SheetDate} / {Division}.", stockItemId, sheetDate.ToString("yyyy-MM-dd"), div);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the stock take entry.");
            }
        }

    }
}
