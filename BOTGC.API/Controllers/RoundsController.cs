using BOTGC.API.Dto;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BOTGC.API.Controllers
{
    /// <summary>
    /// Controller for handling golf round-related operations.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="RoundsController"/> class.
    /// </remarks>
    /// <param name="logger">Logger instance.</param>
    /// <param name="mediator">Mediator to handle queries</param>
    [ApiController]
    [Route("api/rounds")]
    [Produces("application/json")]
    public class RoundsController(ILogger<RoundsController> logger, IMediator mediator) : ControllerBase
    {
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly ILogger<RoundsController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

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
                var query = new GetScorecardForRoundQuery() { RoundId = roundId };
                var scoreCard = await _mediator.Send(query, HttpContext.RequestAborted);

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
