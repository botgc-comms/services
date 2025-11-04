using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using MediatR;
using BOTGC.API.Services.Queries;

namespace BOTGC.API.Controllers
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CompetitionsController"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="reportService">Service handling execution and retrieval of report data.</param>
    [ApiController]
    [Route("api/competitions")]
    [Produces("application/json")]
    public class CompetitionsController(IOptions<AppSettings> settings,
                                  ILogger<CompetitionsController> logger,
                                  IMediator mediator,
                                  ICompetitionTaskQueue taskQueue,
                                  IServiceScopeFactory serviceScopeFactory) : Controller
    {
        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly ICompetitionTaskQueue _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        private readonly ILogger<CompetitionsController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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

        [HttpPost("winnings/calculate")]
        public async Task<IActionResult> CalculateWinnings()
        {
            var command = new ProcessFinalisedCompetitionPayoutsCommand();

            var competitionsProcessed = await _mediator.Send(command, HttpContext.RequestAborted);  

            return Accepted(new { Message = $"Winnings have been calculated for {competitionsProcessed} competitions." });
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

        /// <summary>
        /// Returns competitions filtered by status (active, future, and/or finalised).
        /// </summary>
        /// <remarks>
        /// Defaults to <c>?active=true&amp;future=true&amp;finalised=false</c>.
        /// Examples:
        /// GET /api/stock?active=true&amp;future=true
        /// GET /api/stock?finalised=true
        /// GET /api/stock?active=true&amp;finalised=true
        /// </remarks>
        /// <param name="active">Include currently active competitions. Default true.</param>
        /// <param name="future">Include upcoming competitions. Default true.</param>
        /// <param name="finalised">Include finalised competitions. Default false.</param>
        /// <response code="200">A list of competitions matching the supplied filters.</response>
        /// <response code="204">No competitions matched the supplied filters.</response>
        /// <response code="500">An error occurred while retrieving competitions.</response>
        [HttpGet("")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(List<CompetitionDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCompetitions(
            [FromQuery] bool active = true,
            [FromQuery] bool future = true,
            [FromQuery] bool finalised = false)
        {
            _logger.LogInformation("Fetching competitions with filters => active: {Active}, future: {Future}, finalised: {Finalised}.", active, future, finalised);

            try
            {
                var query = new GetActiveAndFutureCompetitionsQuery(active, future, finalised);
                var competitions = await _mediator.Send(query, HttpContext.RequestAborted);

                if (competitions == null || competitions.Count == 0)
                {
                    _logger.LogWarning("No competitions found for the supplied filters.");
                    return NoContent();
                }

                _logger.LogInformation("Successfully retrieved {Count} competitions.", competitions.Count);
                return Ok(competitions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving competitions.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving competitions.");
            }
        }

        [HttpGet("{competitionId}")]
        public async Task<IActionResult> GetCompetitionDetails(string competitionId)
        {
            _logger.LogInformation($"Fetching competition {competitionId}...");

            try
            {
                var query = new GetCompetitionSettingsByCompetitionIdQuery() { CompetitionId = competitionId };
                var settings = await _mediator.Send(query, HttpContext.RequestAborted);

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
                var query = new GetCompetitionLeaderboardByCompetitionQuery() { CompetitionId = competitionId };
                var leaderboard = await _mediator.Send(query, HttpContext.RequestAborted);

                if (leaderboard == null)
                {
                    _logger.LogWarning($"No leaderboard found for competition {competitionId}.");
                    return NoContent();
                }

                _logger.LogInformation($"Successfully retrieved leaderboard for competition {competitionId}.");
                return Ok(leaderboard);
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
                var query = new GetClubChampionshipLeaderboardByCompetitionQuery() { CompetitionId = competitionId };
                var leaderboard = await _mediator.Send(query, HttpContext.RequestAborted);

                if (leaderboard == null)
                {
                    _logger.LogWarning($"No leaderboard found for competition {competitionId}.");
                    return NoContent();
                }

                _logger.LogInformation($"Successfully retrieved leaderboard for competition {competitionId}.");
                return Ok(leaderboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving leaderboard for competition id {competitionId}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while leaderboard for compeition {competitionId}.");
            }
        }
    }
}
