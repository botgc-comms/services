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
    /// Endpoints for competitions: listings, settings, leaderboards, winnings, and Junior Eclectic results.
    /// </summary>
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

        /// <summary>
        /// Queues a background job to prepare Junior Eclectic results for the given date range.
        /// </summary>
        /// <remarks>
        /// The job is queued for asynchronous processing. Use <c>GET /api/competitions/juniorEclectic/results</c> to retrieve cached results once ready.
        /// </remarks>
        /// <param name="dateRange">Inclusive start and end dates for eligible rounds.</param>
        /// <response code="202">The preparation job was accepted.</response>
        /// <response code="400">The request body was invalid.</response>
        [HttpPost("juniorEclectic/prepare")]
        [Consumes("application/json")]
        [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
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

        /// <summary>
        /// Triggers calculation and persistence of winnings for all finalised competitions not yet processed.
        /// </summary>
        /// <remarks>
        /// Processing runs in parallel (bounded concurrency) and persists results to Azure Table Storage.
        /// </remarks>
        /// <response code="202">The calculation job was accepted. The response includes a count of competitions processed in this run.</response>
        [HttpPost("winnings/calculate")]
        [ProducesResponseType(typeof(object), StatusCodes.Status202Accepted)]
        public async Task<IActionResult> CalculateWinnings()
        {
            var command = new ProcessFinalisedCompetitionPayoutsCommand();
            var competitionsProcessed = await _mediator.Send(command, HttpContext.RequestAborted);
            return Accepted(new { Message = $"Winnings have been calculated for {competitionsProcessed} competitions." });
        }

        /// <summary>
        /// Returns the annual winnings summary for the specified calendar year.
        /// </summary>
        /// <param name="year">Calendar year, e.g. <c>2025</c>.</param>
        /// <returns>Divisional winners per competition, overall top earner, and total prize money paid.</returns>
        /// <response code="200">The annual winnings summary.</response>
        /// <response code="400">The supplied year was invalid.</response>
        [HttpGet("winnings")]
        [Produces("application/json")]
        [ProducesResponseType(typeof(YearlyWinningsSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<YearlyWinningsSummaryDto>> GetWinnings([FromQuery] int year)
        {
            if (year < 2000 || year > DateTime.UtcNow.Year + 1)
            {
                return BadRequest(new { Message = "Please supply a valid calendar year." });
            }

            var dto = await _mediator.Send(new GetYearlyWinningsSummaryQuery(year), HttpContext.RequestAborted);
            return Ok(dto);
        }

        /// <summary>
        /// Returns cached Junior Eclectic results for the specified date range.
        /// </summary>
        /// <param name="fromDate">Start date (inclusive).</param>
        /// <param name="toDate">End date (inclusive).</param>
        /// <response code="200">Results were found in cache.</response>
        /// <response code="404">No results exist for the specified range.</response>
        [HttpGet("juniorEclectic/results")]
        [ProducesResponseType(typeof(EclecticCompetitionResultsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetJuniorEclecticResults([FromQuery] DateTime fromDate, [FromQuery] DateTime toDate)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var key = $"Junior_Eclectic_Results_{fromDate:yyyyMMdd}_{toDate:yyyyMMdd}";
            var results = await cacheService.GetAsync<EclecticCompetitionResultsDto>(key);
            return results != null ? Ok(results) : NotFound();
        }

        /// <summary>
        /// Returns competitions filtered by status (active, future, and/or finalised).
        /// </summary>
        /// <remarks>
        /// Defaults to <c>?active=true&amp;future=true&amp;finalised=false</c>.<br/>
        /// Examples:<br/>
        /// GET <c>/api/competitions?active=true&amp;future=true</c><br/>
        /// GET <c>/api/competitions?finalised=true</c><br/>
        /// GET <c>/api/competitions?active=true&amp;finalised=true</c>
        /// </remarks>
        /// <param name="active">Include currently active competitions. Default <c>true</c>.</param>
        /// <param name="future">Include upcoming competitions. Default <c>true</c>.</param>
        /// <param name="finalised">Include finalised competitions. Default <c>false</c>.</param>
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

        /// <summary>
        /// Returns settings and signup configuration for a specific competition.
        /// </summary>
        /// <param name="competitionId">Competition identifier.</param>
        /// <response code="200">Settings were found.</response>
        /// <response code="204">No settings found for the supplied identifier.</response>
        /// <response code="500">An error occurred while retrieving settings.</response>
        [HttpGet("{competitionId}")]
        [ProducesResponseType(typeof(CompetitionSettingsDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCompetitionDetails(string competitionId)
        {
            _logger.LogInformation("Fetching competition {CompetitionId}...", competitionId);

            try
            {
                var query = new GetCompetitionSettingsByCompetitionIdQuery { CompetitionId = competitionId };
                var settings = await _mediator.Send(query, HttpContext.RequestAborted);

                if (settings == null)
                {
                    _logger.LogWarning("No competitions found.");
                    return NoContent();
                }

                _logger.LogInformation("Successfully retrieved settings for competition {CompetitionId}.", competitionId);
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving competition settings for competition id {CompetitionId}.", competitionId);
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while retrieving settings for competition {competitionId}.");
            }
        }

        /// <summary>
        /// Returns the leaderboard for a specific competition.
        /// </summary>
        /// <param name="competitionId">Competition identifier.</param>
        /// <response code="200">Leaderboard was found.</response>
        /// <response code="204">No leaderboard found for the supplied identifier.</response>
        /// <response code="500">An error occurred while retrieving the leaderboard.</response>
        [HttpGet("{competitionId}/leaderboard")]
        [ProducesResponseType(typeof(LeaderBoardDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCompetitionLeaderboard(string competitionId)
        {
            _logger.LogInformation("Fetching leaderboard for competition {CompetitionId}...", competitionId);

            try
            {
                var query = new GetCompetitionLeaderboardByCompetitionQuery { CompetitionId = competitionId };
                var leaderboard = await _mediator.Send(query, HttpContext.RequestAborted);

                if (leaderboard == null)
                {
                    _logger.LogWarning("No leaderboard found for competition {CompetitionId}.", competitionId);
                    return NoContent();
                }

                _logger.LogInformation("Successfully retrieved leaderboard for competition {CompetitionId}.", competitionId);
                return Ok(leaderboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving leaderboard for competition id {CompetitionId}.", competitionId);
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while retrieving the leaderboard for competition {competitionId}.");
            }
        }

        /// <summary>
        /// Returns the Club Championships leaderboard (two rounds and aggregate) for a specific competition.
        /// </summary>
        /// <param name="competitionId">Competition identifier.</param>
        /// <response code="200">Club Championships leaderboard was found.</response>
        /// <response code="204">No leaderboard found for the supplied identifier.</response>
        /// <response code="500">An error occurred while retrieving the leaderboard.</response>
        [HttpGet("{competitionId}/clubChampionshipsLeaderboard")]
        [ProducesResponseType(typeof(ClubChampionshipLeaderBoardDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetClubChampionshipsCompetitionLeaderboard(string competitionId)
        {
            _logger.LogInformation("Fetching leaderboard for competition {CompetitionId}...", competitionId);

            try
            {
                var query = new GetClubChampionshipLeaderboardByCompetitionQuery { CompetitionId = competitionId };
                var leaderboard = await _mediator.Send(query, HttpContext.RequestAborted);

                if (leaderboard == null)
                {
                    _logger.LogWarning("No leaderboard found for competition {CompetitionId}.", competitionId);
                    return NoContent();
                }

                _logger.LogInformation("Successfully retrieved leaderboard for competition {CompetitionId}.", competitionId);
                return Ok(leaderboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving leaderboard for competition id {CompetitionId}.", competitionId);
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while retrieving the leaderboard for competition {competitionId}.");
            }
        }
    }
}
