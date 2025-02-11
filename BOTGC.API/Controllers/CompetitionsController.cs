using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Dto;
using Services.Interfaces;
using Services.Models;
using Services.Services;
using System.Runtime;
using System.Threading;

namespace Services.Controllers
{
    [ApiController]
    [Route("api/competitions")]
    [Produces("application/json")]
    public class CompetitionsController : Controller
    {
        private readonly AppSettings _settings;
        private readonly IDataService _reportService;
        private readonly ICompetitionTaskQueue _taskQueue;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<CompetitionsController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompetitionsController"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="reportService">Service handling execution and retrieval of report data.</param>
        public CompetitionsController(IOptions<AppSettings> settings,
                                      ILogger<CompetitionsController> logger,
                                      IDataService reportService,
                                      ICompetitionTaskQueue taskQueue,
                                      IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        }

        [HttpPost("juniorEclectic/prepare")]
        public async Task<IActionResult> PrepareJuniorEclecticResults([FromBody] DateRange dateRange)
        {
            var taskItem = new CompetitionTaskItem
            {
                FromDate = dateRange.Start, 
                ToDate = dateRange.End, 
                CompetitionType = "JuniorEclectic"
            };

            await _taskQueue.QueueTaskAsync(taskItem);

            return Accepted(new { Message = "Competition status retrieval started." });
        }

        [HttpGet("juniorEclectic/results")]
        public async Task<IActionResult> GetJuniorEclecticResults([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var results = await cacheService.GetAsync<EclecticCompetitionResultsDto>($"Junior_Eclectic_Results_{fromDate.ToString("yyyyMMdd")}_{toDate.ToString("yyyyMMdd")}");
            if (results != null)
            {
                return Ok(results);
            }
            else
            {
                return NotFound();
            }
        }
    }
}
