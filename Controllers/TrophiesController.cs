using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Services.Dtos;

namespace Servicesx.Controllers
{
    /// <summary>
    /// API for managing trophies and their data.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class TrophiesController : ControllerBase
    {
        private readonly ITrophyService _trophyService;
        private readonly ILogger<TrophiesController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrophiesController"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="trophyService">Service handling trophy data retrieval.</param>
        public TrophiesController(ILogger<TrophiesController> logger, ITrophyService trophyService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _trophyService = trophyService ?? throw new ArgumentNullException(nameof(trophyService));
        }

        /// <summary>
        /// Retrieves a list of all available trophies.
        /// </summary>
        /// <returns>A list of trophy IDs sorted alphabetically.</returns>
        /// <response code="200">Successfully retrieved the list of trophies.</response>
        /// <response code="404">No trophies found.</response>
        /// <response code="500">Internal server error occurred.</response>
        [HttpGet(Name = "ListTrophies")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<string>))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult<IReadOnlyCollection<string>>> GetTrophiesAsync()
        {
            _logger.LogInformation("Fetching list of trophies...");
            try
            {
                var trophies = await _trophyService.ListTrophyIdsAsync();

                if (trophies.Count == 0)
                {
                    _logger.LogWarning("No trophies found.");
                    return NotFound(new ProblemDetails
                    {
                        Title = "No Trophies Found",
                        Detail = "The trophy data store does not contain any trophies.",
                        Status = StatusCodes.Status404NotFound,
                        Instance = HttpContext.Request.Path
                    });
                }

                _logger.LogInformation("Successfully retrieved {Count} trophies.", trophies.Count);
                return Ok(trophies);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching trophies.");
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An error occurred while retrieving trophy data.",
                    Status = StatusCodes.Status500InternalServerError,
                    Instance = HttpContext.Request.Path
                });
            }
        }

        /// <summary>
        /// Retrieves details of a specific trophy by its slug, including navigation to previous and next trophies.
        /// </summary>
        /// <param name="id">The unique slug of the trophy.</param>
        /// <returns>Trophy details with links to previous and next trophies.</returns>
        /// <response code="200">Successfully retrieved the trophy details.</response>
        /// <response code="404">Trophy not found.</response>
        /// <response code="500">Internal server error.</response>
        [HttpGet("{id}", Name = "GetTrophyById")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TrophyDto))]
        [ProducesResponseType(StatusCodes.Status404NotFound, Type = typeof(ProblemDetails))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError, Type = typeof(ProblemDetails))]
        public async Task<ActionResult<TrophyDto>> GetTrophyByIdAsync(string id)
        {
            _logger.LogInformation("Fetching trophy details for ID: {Id}", id);
            try
            {
                var allTrophyIds = (await _trophyService.ListTrophyIdsAsync()).ToList();

                if (!allTrophyIds.Any())
                {
                    _logger.LogWarning("No trophies exist in the data store.");
                    return NotFound(new ProblemDetails
                    {
                        Title = "No Trophies Found",
                        Detail = "The trophy data store does not contain any trophies.",
                        Status = StatusCodes.Status404NotFound,
                        Instance = HttpContext.Request.Path
                    });
                }

                // Ensure the requested trophy exists in the list
                var currentIndex = allTrophyIds.IndexOf(id);
                if (currentIndex == -1)
                {
                    _logger.LogWarning("Trophy with ID {Id} not found.", id);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Trophy Not Found",
                        Detail = $"Trophy with ID '{id}' does not exist.",
                        Status = StatusCodes.Status404NotFound,
                        Instance = HttpContext.Request.Path
                    });
                }

                // Determine the previous and next trophies
                var previousIndex = (currentIndex == 0) ? allTrophyIds.Count - 1 : currentIndex - 1;
                var nextIndex = (currentIndex == allTrophyIds.Count - 1) ? 0 : currentIndex + 1;

                var previousSlug = allTrophyIds[previousIndex];
                var nextSlug = allTrophyIds[nextIndex];

                _logger.LogInformation("Trophy {Id} found. Previous: {PreviousSlug}, Next: {NextSlug}", id, previousSlug, nextSlug);

                // Get the requested trophy metadata
                var trophy = await _trophyService.GetTrophyByIdAsync(id);
                if (trophy == null)
                {
                    _logger.LogWarning("Trophy metadata for ID {Id} is missing.", id);
                    return NotFound(new ProblemDetails
                    {
                        Title = "Trophy Metadata Not Found",
                        Detail = $"Trophy with ID '{id}' exists but metadata is missing.",
                        Status = StatusCodes.Status404NotFound,
                        Instance = HttpContext.Request.Path
                    });
                }

                var trophyDto = new TrophyDto
                {
                    Slug = trophy.Slug,
                    Name = trophy.Name,
                    Description = trophy.Description,
                    Links = new Dictionary<string, string>
                    {
                        { "self", $"{Request.Scheme}://{Request.Host}/api/trophies/{trophy.Slug}" },
                        { "previous", $"{Request.Scheme}://{Request.Host}/api/trophies/{previousSlug}" },
                        { "next", $"{Request.Scheme}://{Request.Host}/api/trophies/{nextSlug}" },
                        { "winnerImage", $"{Request.Scheme}://{Request.Host}/api/images/winners/{trophy.Slug}" }
                    }
                };

                return Ok(trophyDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching trophy details for ID: {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, new ProblemDetails
                {
                    Title = "Internal Server Error",
                    Detail = "An error occurred while retrieving the trophy details.",
                    Status = StatusCodes.Status500InternalServerError,
                    Instance = HttpContext.Request.Path
                });
            }
        }
    }
}




