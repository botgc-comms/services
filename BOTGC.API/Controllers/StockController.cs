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
