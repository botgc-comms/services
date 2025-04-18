using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services;
using System.Runtime;
using System.Threading;

namespace BOTGC.API.Controllers
{
    [ApiController]
    [Route("api/eventLog")]
    [Produces("application/json")]
    public class EventLogController : Controller
    {
        private readonly AppSettings _settings;
        private readonly IDataService _reportService;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<CompetitionsController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompetitionsController"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="reportService">Service handling execution and retrieval of report data.</param>
        public EventLogController(IOptions<AppSettings> settings,
                                    ILogger<CompetitionsController> logger,
                                    IDataService reportService,
                                    IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        [HttpGet("mobileOrders")]
        public async Task<IActionResult> GetMobileOrders([FromQuery] DateTime? forDate = null)
        {
            _logger.LogInformation($"Fetching mobile orders...");

            try
            {
                var mobileOrders = await _reportService.GetMobileOrders(forDate);

                if (mobileOrders != null && mobileOrders.Any())
                {
                    _logger.LogInformation($"Successfully retrieved {mobileOrders.Count} mobile orders.");
                }

                return Ok(mobileOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving mobile orders.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while retrieving mobile orders.");
            }
        }

    }
}
