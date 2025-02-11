using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Services.Dto;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services.Controllers
{
    /// <summary>
    /// Controller for handling golf round-related operations.
    /// </summary>
    [ApiController]
    [Route("api/rounds")]
    [Produces("application/json")]
    public class RoundsController : ControllerBase
    {
        private readonly IDataService _reportService;
        private readonly ILogger<RoundsController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoundsController"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="reportService">Service for handling round reports.</param>
        public RoundsController(ILogger<RoundsController> logger, IDataService reportService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        }

        /// <summary>
        /// Retrieves the scorecard for a given round ID.
        /// </summary>
        /// <param name="roundId">The unique identifier of the round.</param>
        /// <returns>The scorecard for the specified round.</returns>
        /// <response code="200">Returns the scorecard for the given round ID.</response>
        /// <response code="204">No scorecard found for the given round ID.</response>
        /// <response code="500">An internal server error occurred.</response>
        [HttpGet("{roundId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ScorecardDto))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ScorecardDto>> GetScorecardForRoundId(string roundId)
        {
            _logger.LogInformation("Fetching scorecard for round {RoundId}...", roundId);

            try
            {
                var scoreCard = await _reportService.GetScorecardForRoundAsync(roundId);

                if (scoreCard == null)
                {
                    _logger.LogWarning("No scorecard was found for round {RoundId}.", roundId);
                    return NoContent();
                }

                _logger.LogInformation("Successfully retrieved scorecard for round {RoundId}.", roundId);
                return Ok(scoreCard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving scorecard for round {RoundId}.", roundId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the scorecard.");
            }
        }
    }
}
