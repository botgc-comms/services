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

        [HttpGet("")]
        public async Task<IActionResult> GetCurrentAndFutureCompetitions()
        {
            _logger.LogInformation($"Fetching current and future competitions...");

            try
            {
                var competitions = await _reportService.GetActiveAndFutureCompetitionsAsync();

                if (competitions == null || competitions.Count == 0)
                {
                    _logger.LogWarning($"No competitions found.");
                    return NoContent();
                }

                _logger.LogInformation($"Successfully retrieved {competitions.Count} competitions.");
                return Ok(competitions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving competitions.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while retrieving current and future competitions.");
            }
        }

        [HttpGet("{competitionId}")]
        public async Task<IActionResult> GetCompetitionDetails(string competitionId)
        {
            _logger.LogInformation($"Fetching competition {competitionId}...");

            try
            {
                var settings = await _reportService.GetCompetitionSettingsAsync(competitionId);

                if (settings == null)
                {
                    _logger.LogWarning($"No competitions found.");
                    return NoContent();
                }

                _logger.LogInformation($"Successfully retrieved settings for competition {competitionId}.");
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving competition settings for competition id {competitionId}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while retrieving settings for compeition {competitionId}.");
            }
        }

        [HttpGet("{competitionId}/leaderboard")]
        public async Task<IActionResult> GetCompetitionLeaderboard(string competitionId)
        {
            _logger.LogInformation($"Fetching leaderboard for competition {competitionId}...");

            try
            {
                var settings = await _reportService.GetCompetitionLeaderboardAsync(competitionId);

                if (settings == null)
                {
                    _logger.LogWarning($"No leaderboard found for competition {competitionId}.");
                    return NoContent();
                }

                _logger.LogInformation($"Successfully retrieved leaderboard for competition {competitionId}.");
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving leaderboard for competition id {competitionId}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while leaderboard for compeition {competitionId}.");
            }
        }

        [HttpGet("{competitionId}/clubChampionshipsLeaderboard")]
        public async Task<IActionResult> GetClubChampionshipsCompetitionLeaderboard(string competitionId)
        {
            _logger.LogInformation($"Fetching leaderboard for competition {competitionId}...");

            try
            {
                var settings = await _reportService.GetClubChampionshipsLeaderboardAsync(competitionId);

                if (settings == null)
                {
                    _logger.LogWarning($"No leaderboard found for competition {competitionId}.");
                    return NoContent();
                }

                _logger.LogInformation($"Successfully retrieved leaderboard for competition {competitionId}.");
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving leaderboard for competition id {competitionId}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while leaderboard for compeition {competitionId}.");
            }
        }
    }
}
