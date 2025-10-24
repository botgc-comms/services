// Controllers/StockController.cs — fully annotated
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Controllers
{
    /// <summary>
    /// Stock and stock-take operations for BOTGC.
    /// </summary>
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

        /// <summary>
        /// Gets the current stock levels.
        /// </summary>
        /// <returns>A list of stock items with current levels.</returns>
        [HttpGet("stockLevels")]
        [ProducesResponseType(typeof(List<StockItemDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetStockLevels()
        {
            _logger.LogInformation("Fetching current stock levels...");

            try
            {
                var query = new GetStockLevelsQuery();
                var stockItems = await _mediator.Send(query, HttpContext.RequestAborted);

                if (stockItems == null || stockItems.Count == 0)
                {
                    _logger.LogWarning("No stock items were found.");
                    return NoContent();
                }

                _logger.LogInformation("Successfully retrieved {Count} stock items.", stockItems.Count);
                return Ok(stockItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock items.");
                return Problem("An error occurred while retrieving stock items.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Gets product settings
        /// </summary>
        /// <returns>A product settings for the given produc tid.</returns>
        [HttpGet("products/{id}")]
        [ProducesResponseType(typeof(TillProductInformationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetProduct([FromRoute] int id)
        {
            _logger.LogInformation("Fetching product information for product {ProductId}...", id);  

            try
            {
                var query = new GetTillProductInformationQuery(ProductId: id);
                var productInformation = await _mediator.Send(query, HttpContext.RequestAborted);

                if (productInformation == null)
                {
                    _logger.LogWarning("No product information found for product {ProductId}.", id);    
                    return NoContent();
                }

                _logger.LogInformation("Successfully retrieved product information for product {ProductId}.", id);  
                return Ok(productInformation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving product information for product {ProductId}.", id);  
                return Problem("An error occurred while retrieving product information.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Gets product settings
        /// </summary>
        /// <returns>A product settings for the given produc tid.</returns>
        [HttpGet("products")]
        [ProducesResponseType(typeof(TillProductInformationDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetProducts()
        {
            _logger.LogInformation("Fetching all products");

            try
            {
                var query = new GetTillProductsQuery();
                var products = await _mediator.Send(query, HttpContext.RequestAborted);

                if (products == null && products!.Any())
                {
                    _logger.LogWarning("No products found.");   
                    return NoContent();
                }

                _logger.LogInformation($"Successfully retrieved {products.Count} products.");
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving products."); 
                return Problem("An error occurred while retrieving products.", statusCode: StatusCodes.Status500InternalServerError);   
            }
        }

        /// <summary>
        /// Gets the stock catalogue including available trade units.
        /// </summary>
        /// <returns>Stock items with trade unit information.</returns>
        [HttpGet("stockItems")]
        [ProducesResponseType(typeof(List<StockItemAndTradeUnitDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetStockItems()
        {
            _logger.LogInformation("Fetching current stock items...");

            try
            {
                var query = new GetStockItemsAndTradeUnitsQuery();
                var stockItems = await _mediator.Send(query, HttpContext.RequestAborted);

                if (stockItems == null || stockItems.Count == 0)
                {
                    _logger.LogWarning("No stock items were found.");
                    return NoContent();
                }

                _logger.LogInformation("Successfully retrieved {Count} stock items.", stockItems.Count);
                return Ok(stockItems);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock items.");
                return Problem("An error occurred while retrieving stock items.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Gets the stock catalogue including available trade units.
        /// </summary>
        /// <returns>Stock items with trade unit information.</returns>
        [HttpPost("stockItems")]
        [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> AddStockItems([FromBody] CreateStockItemCommand command)
        {
            _logger.LogInformation("Fetching current stock items...");

            try
            {
                var stockItemId = await _mediator.Send(command, HttpContext.RequestAborted);

                if (stockItemId == null)
                {
                    _logger.LogWarning("Failed to create Stock Item {name}.", command.Name);
                    return NoContent();
                }

                _logger.LogInformation("Successfully created stock item {StockItemName}: {StockItemId}.", command.Name, stockItemId);  
                return Ok(stockItemId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating stock item.");
                return Problem("An error occurred while creating stock item.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Gets the list of till operators.
        /// </summary>
        [HttpGet("tillOperators")]
        [ProducesResponseType(typeof(List<TillOperatorDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        public async Task<IActionResult> GetTillOperators()
        {
            _logger.LogInformation("Fetching current till operators...");

            try
            {
                var query = new GetTillOperatorsQuery();
                var tillOperators = await _mediator.Send(query, HttpContext.RequestAborted);

                if (tillOperators == null || tillOperators.Count == 0)
                {
                    _logger.LogWarning("No till operators were found.");
                    return NoContent();
                }

                _logger.LogInformation("Successfully retrieved {Count} till operators.", tillOperators.Count);
                return Ok(tillOperators);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving till operators.");
                return Problem("An error occurred while retrieving till operators.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Gets the waste sheet for a specific day.
        /// </summary>
        /// <param name="day">UTC date (yyyy-MM-dd). Defaults to today if omitted.</param>
        [HttpGet("wasteSheet")]
        [ProducesResponseType(typeof(WasteSheetDto), StatusCodes.Status200OK)]
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
                return Problem("An error occurred while retrieving the waste sheet.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Adds an entry to the waste sheet for the specified day.
        /// </summary>
        /// <param name="request">Waste entry details.</param>
        /// <param name="day">UTC date (yyyy-MM-dd). Defaults to today if omitted.</param>
        [HttpPost("wasteSheet/entry")]
        [ProducesResponseType(typeof(AddResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
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
                    return Ok(result);
                }

                return Created($"/api/stock/wasteSheet?day={sheetDate:yyyy-MM-dd}", new { ok = true, duplicated = false });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding waste entry for {SheetDate}.", sheetDate.ToString("yyyy-MM-dd"));
                return Problem("An error occurred while adding the waste entry.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Updates a stock item by ID.
        /// </summary>
        /// <param name="id">Stock item ID (route).</param>
        /// <param name="cmd">Update payload; <c>StockId</c> must match the route ID.</param>
        [HttpPut("stockItems/{id}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status499ClientClosedRequest)]
        public async Task<IActionResult> UpdateStockItem([FromRoute] string id, [FromBody] UpdateStockItemCommand cmd)
        {
            using var _ = _logger.BeginScope(new Dictionary<string, object?> { ["RouteId"] = id });

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

            if (cmd is null)
            {
                _logger.LogWarning("Request body is null for UpdateStockItem {RouteStockId}.", routeStockId);
                return BadRequest(new { message = "Request body cannot be null." });
            }

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
                return Problem("An error occurred while updating the stock item.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Deletes a waste entry by ID for a specific day.
        /// </summary>
        /// <param name="id">Waste entry ID.</param>
        /// <param name="day">UTC date (yyyy-MM-dd). Defaults to today if omitted.</param>
        [HttpDelete("wasteSheet/entry/{id}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
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
                return Problem("An error occurred while deleting the waste entry.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Gets historic stock takes, optionally filtered by date range.
        /// </summary>
        /// <param name="fromDate">Inclusive start date (UTC).</param>
        /// <param name="toDate">Inclusive end date (UTC).</param>
        [HttpGet("stockTakes")]
        [ProducesResponseType(typeof(List<StockTakeReportEntryDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetStockTakes([FromQuery] DateTime? fromDate = null, [FromQuery] DateTime? toDate = null)
        {
            _logger.LogInformation("Fetching previous stock takes...");

            try
            {
                var query = new GetStockTakesQuery
                {
                    FromDate = fromDate,
                    ToDate = toDate
                };

                var products = await _mediator.Send(query, HttpContext.RequestAborted);

                if (products == null || products.Count == 0)
                {
                    _logger.LogInformation("No previous stock takes were found.");
                    return Ok(new List<StockTakeReportEntryDto>());
                }

                _logger.LogInformation("Successfully retrieved {Count} products involved in previous stock takes.", products.Count);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving previous stock takes.");
                return Problem("An error occurred while retrieving previous stock takes.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Gets the prepared product list for a stock take.
        /// </summary>
        [HttpGet("stockTakes/products")]
        [ProducesResponseType(typeof(List<StockTakeReportEntryDto>), StatusCodes.Status200OK)]
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

                _logger.LogInformation("Successfully retrieved {Count} products for the stock take.", products.Count);
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving stock take product list.");
                return Problem("An error occurred while retrieving the stock take product list.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Queues an asynchronous stock analysis task.
        /// </summary>
        /// <param name="request">Analysis task details.</param>
        [HttpPost("analyse")]
        [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> QueueStockAnalysis([FromBody] StockAnalysisTaskItem request)
        {
            _logger.LogInformation("Received request to queue stock analysis.");

            if (request == null)
            {
                _logger.LogWarning("No task data supplied in the request body.");
                return BadRequest(new ProblemDetails { Title = "Validation error", Detail = "Missing task data." });
            }

            await _taskQueue.QueueTaskAsync(request);
            return Accepted(new { message = "Stock analysis task queued." });
        }

        /// <summary>
        /// Gets a stock take sheet for a specific day and division.
        /// </summary>
        /// <param name="day">UTC date (yyyy-MM-dd). Defaults to today.</param>
        /// <param name="division">Division filter (e.g. MINERALS). Optional.</param>
        [HttpGet("stockTakes/sheet")]
        [ProducesResponseType(typeof(StockTakeSheetDto), StatusCodes.Status200OK)]
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
                return Problem("An error occurred while retrieving the stock take sheet.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Creates or updates an entry on the stock take sheet.
        /// </summary>
        /// <param name="request">Entry details to upsert.</param>
        /// <param name="day">UTC date (yyyy-MM-dd). Defaults to today.</param>
        [HttpPost("stockTakes/sheet/entry")]
        [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
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
                    request.Observations?.Select(o => new StockTakeObservationDto(o.StockItemId, o.Code, o.Location, o.Value)).ToList() ?? new List<StockTakeObservationDto>(),
                    request.EstimatedQuantityAtCapture
                );

                var _ = await _mediator.Send(cmd, HttpContext.RequestAborted);

                return Created($"/api/stock/stockTakes/sheet?day={sheetDate:yyyy-MM-dd}&division={Uri.EscapeDataString(request.Division ?? string.Empty)}",
                               new { ok = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error upserting stock take entry for {SheetDate}.", sheetDate.ToString("yyyy-MM-dd"));
                return Problem("An error occurred while saving the stock take entry.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Creates a purchase order from stock take recommendations.
        /// </summary>
        /// <param name="request">Purchase order details.</param>
        [HttpPost("stockTakes/purchaseOrder")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public async Task<IActionResult> TestCreatePurchaseOrder([FromBody] PurchaseOrderDto request)
        {
            try
            {
                var cmd = new CreatePurchaseOrderCommand(request);
                var _ = await _mediator.Send(cmd, HttpContext.RequestAborted);
                return Ok(new { ok = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating purchase order.");
                return Problem("An error occurred creating the purchase order.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }

        /// <summary>
        /// Deletes a stock take entry by stock item ID for a day and division.
        /// </summary>
        /// <param name="stockItemId">Stock item ID.</param>
        /// <param name="day">UTC date (yyyy-MM-dd). Defaults to today.</param>
        /// <param name="division">Division name. Optional.</param>
        [HttpDelete("stockTakes/sheet/entry/{stockItemId:int}")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteStockTakeEntry([FromRoute] int stockItemId, [FromQuery] DateTime? day = null, [FromQuery] string? division = null)
        {
            var sheetDate = (day?.Date ?? DateTime.UtcNow.Date);
            var div = division ?? string.Empty;

            _logger.LogInformation("Deleting stock take entry {StockItemId} from sheet {SheetDate} / {Division}.", stockItemId, sheetDate.ToString("yyyy-MM-dd"), div);

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
                return Problem("An error occurred while deleting the stock take entry.", statusCode: StatusCodes.Status500InternalServerError);
            }
        }
    }
}
