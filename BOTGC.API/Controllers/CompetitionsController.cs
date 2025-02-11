using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Dto;
using Services.Interfaces;
using Services.Models;
using Services.Services;
using System.Runtime;

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
        private readonly ILogger<CompetitionsController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompetitionsController"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="reportService">Service handling execution and retrieval of report data.</param>
        public CompetitionsController(IOptions<AppSettings> settings,
                                      ILogger<CompetitionsController> logger,
                                      IDataService reportService,
                                      ICompetitionTaskQueue taskQueue)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        }

        [HttpPost("retrieve")]
        public async Task<IActionResult> RetrieveCompetitionStatus([FromBody] DateRange dateRange)
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

    }
}
