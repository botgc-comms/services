using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Dto;
using Services.Interfaces;
using Services.Services;
using System.Runtime;

namespace Services.Controllers
{
    [ApiController]
    [Route("api/members")]
    [Produces("application/json")]
    public class MembersController : Controller
    {
        private const string __CACHE_JUNIORMEMBERS = "Junior_Members";

        private readonly AppSettings _settings;
        private readonly IReportService _reportService;
        private readonly ICacheService _cacheService;
        private readonly ILogger<MembersController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MembersController"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="reportService">Service handling execution and retrieval of report data.</param>
        public MembersController(IOptions<AppSettings> settings,
                                 ILogger<MembersController> logger,
                                 IReportService reportService,
                                 ICacheService cacheService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
        }

        /// <summary>
        /// Retrieves a list of all junior members.
        /// </summary>
        /// <returns>A list of junior members with their details.</returns>
        /// <response code="200">Returns the list of junior members.</response>
        /// <response code="204">No junior members found.</response>
        /// <response code="500">An internal server error occurred.</response>
        [HttpGet("junior-members")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<MemberDto>))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyCollection<MemberDto>>> GetJuniorMembers()
        {
            _logger.LogInformation("Fetching junior members...");

            try
            {
                var cachedJuniorMembers = await _cacheService.GetAsync<List<MemberDto>>(__CACHE_JUNIORMEMBERS);
                if (cachedJuniorMembers != null && cachedJuniorMembers.Any())
                {
                    _logger.LogInformation($"Retrieved {cachedJuniorMembers.Count()} junior members from cache.");
                    return Ok(cachedJuniorMembers);
                }

                var juniorMembers = await _reportService.GetJuniorMembersAsync();

                await _cacheService.SetAsync(__CACHE_JUNIORMEMBERS, juniorMembers, TimeSpan.FromMinutes(_settings.Cache.TTL_mins));

                if (juniorMembers == null || juniorMembers.Count == 0)
                {
                    _logger.LogWarning("No junior members found.");
                    return NoContent();
                }

                _logger.LogInformation("Successfully retrieved {Count} junior members.", juniorMembers.Count);
                return Ok(juniorMembers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving junior members.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving junior members.");
            }
        }
    }
}
