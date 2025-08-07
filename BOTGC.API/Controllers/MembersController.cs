using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services;
using System.Runtime;
using BOTGC.API.Common;
using MediatR;
using BOTGC.API.Services.Queries;

namespace BOTGC.API.Controllers
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MembersController"/> class.
    /// </summary>
    /// <param name="logger">Logger instance.</param>
    /// <param name="reportService">Service handling execution and retrieval of report data.</param>
    [ApiController]
    [Route("api/members")]
    [Produces("application/json")]
    public class MembersController(IOptions<AppSettings> settings,
                             ILogger<MembersController> logger,
                             IMediator mediator,
                             IMembershipReportingService membershipReporting,
                             IQueueService<NewMemberApplicationDto> memberApplicationQueueService) : Controller
    {
        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly IMembershipReportingService _membershipReporting = membershipReporting ?? throw new ArgumentNullException(nameof(membershipReporting));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly IQueueService<NewMemberApplicationDto> _memberApplicationQueueService = memberApplicationQueueService ?? throw new ArgumentNullException(nameof(memberApplicationQueueService));
        private readonly ILogger<MembersController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        /// <summary>
        /// Retrieves the membership report.
        /// </summary>
        /// <returns>Retrieves the Membership menagement report</returns>
        /// <response code="200">Returns the report data.</response>
        /// <response code="204">No members found.</response>
        /// <response code="500">An internal server error occurred.</response>
        [HttpGet("report")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<MembershipReportDto>))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MembershipReportDto>> GetMembershipReport()
        {
            _logger.LogInformation("Fetching membership report");

            try
            {
                var membershipReport = await _membershipReporting.GetManagementReport(HttpContext.RequestAborted);

                if (membershipReport == null)
                {
                    _logger.LogWarning("Failed to generate management report.");
                    return NoContent();
                }

                _logger.LogInformation("Successfully generated the management report.");
                return Ok(membershipReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving the management report for membership.");
                return StatusCode(StatusCodes.Status500InternalServerError, "Error retrieving the management report for membership.");
            }
        }

        /// <summary>
        /// Retrieves a list of all junior members.
        /// </summary>
        /// <returns>A list of junior members with their details.</returns>
        /// <response code="200">Returns the list of junior members.</response>
        /// <response code="204">No junior members found.</response>
        /// <response code="500">An internal server error occurred.</response>
        [HttpGet("juniors")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<MemberDto>))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyCollection<MemberDto>>> GetJuniorMembers()
        {
            _logger.LogInformation("Fetching junior members...");

            try
            {
                var query = new GetJuniorMembersQuery();
                var juniorMembers = await _mediator.Send(query, HttpContext.RequestAborted);

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

        /// <summary>
        /// Retrieves a list current membership categories
        /// </summary>
        /// <returns>A list of current membership categories.</returns>
        /// <response code="200">Returns the list of current membership categories.</response>
        /// <response code="204">No membership categories found.</response>
        /// <response code="500">An internal server error occurred.</response>
        [HttpGet("categories")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<MembershipCategoryGroupDto>))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyCollection<MembershipCategoryGroupDto>>> GetMembershipCategories()
        {
            _logger.LogInformation("Fetching current membership categories...");

            try
            {
                var query = new GetMembershipCategoriesQuery();
                var memberhipCategories = await _mediator.Send(query, HttpContext.RequestAborted);

                if (memberhipCategories == null || memberhipCategories.Count == 0)
                {
                    _logger.LogWarning("No current membersship categories found.");
                    return NoContent();
                }

                _logger.LogInformation("Successfully retrieved {Count} current membership categories.", memberhipCategories.Count);
                return Ok(memberhipCategories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current members.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving membership categories.");
            }
        }


        /// <summary>
        /// Retrieves a list of all current members.
        /// </summary>
        /// <returns>A list of current members with their details.</returns>
        /// <response code="200">Returns the list of current members.</response>
        /// <response code="204">No current members found.</response>
        /// <response code="500">An internal server error occurred.</response>
        [HttpGet("current")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<MemberDto>))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyCollection<MemberDto>>> GetCurrentMembers()
        {
            _logger.LogInformation("Fetching current members...");

            try
            {
                var query = new GetCurrentMembersQuery();
                var currentMembers = await _mediator.Send(query, HttpContext.RequestAborted);

                if (currentMembers == null || currentMembers.Count == 0)
                {
                    _logger.LogWarning("No current members found.");
                    return NoContent();
                }

                _logger.LogInformation("Successfully retrieved {Count} current members.", currentMembers.Count);
                return Ok(currentMembers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current members.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving current members.");
            }
        }

        /// <summary>
        /// Retrieves a list of all junior members.
        /// </summary>
        /// <returns>A list of junior members with their details.</returns>
        /// <response code="200">Returns the list of junior members.</response>
        /// <response code="204">No junior members found.</response>
        /// <response code="500">An internal server error occurred.</response>
        [HttpGet("{memberId}/rounds")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<RoundDto>))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyCollection<RoundDto>>> GetRoundsByMember(string memberId)
        {
            _logger.LogInformation($"Fetching rounds for member {memberId}...");

            try
            {
                var query = new GetRoundsByMemberIdQuery() { MemberId = memberId };
                var memberRounds = await _mediator.Send(query, HttpContext.RequestAborted);

                if (memberRounds == null || memberRounds.Count == 0)
                {
                    _logger.LogWarning($"No rounds found for member {memberId}.");
                    return NoContent();
                }

                _logger.LogInformation($"Successfully retrieved {memberRounds.Count} rounds for member {memberId}.");
                return Ok(memberRounds);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "No player found for member ID {MemberId}", memberId);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving rounds for member members {memberId}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while retrieving rounds for member {memberId}.");
            }
        }

        /// <summary>
        /// Retrieve subscription payment details
        /// </summary>
        /// <returns>A list of junior members with their details.</returns>
        /// <response code="200">Returns the list of subscription payments for a given subscription year.</response>
        /// <response code="204">No subscription payments found.</response>
        /// <response code="500">An internal server error occurred.</response>
        [HttpGet("subscriptionPayments")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<SubscriptionPaymentDto>))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyCollection<SubscriptionPaymentDto>>> GetSubscriptionPayments(string? subscriptionYear = null)
        {
            subscriptionYear = DateHelper.GetSubscriptionYear(subscriptionYear, DateTime.Now);

            _logger.LogInformation($"Fetching subscription payments for the subscription year {subscriptionYear}...");

            try
            {
                var (startDate, endDate) = DateHelper.GetSubscriptionYearRange(subscriptionYear);

                var query = new GetSubscriptionPaymentsByDateRangeQuery() { FromDate = startDate, ToDate = endDate };
                var subscriptionPayments = await _mediator.Send(query, HttpContext.RequestAborted);

                if (subscriptionPayments == null || subscriptionPayments.Count == 0)
                {
                    _logger.LogWarning($"No subscription payments could be found for subscription year {subscriptionYear}.");
                    return NoContent();
                }

                _logger.LogInformation($"Successfully retrieved {subscriptionPayments.Count} subscription payments for subscription year {subscriptionPayments}.");
                return Ok(subscriptionPayments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving subscription payments for subscription year {subscriptionYear}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving subscription payments for subscription year {subscriptionYear}.");
            }
        }

        [HttpPost("application")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> MemberApplication([FromBody] NewMemberApplicationDto newMember)
        {
            await _memberApplicationQueueService.EnqueueAsync(newMember);

            return Ok(newMember);
        }
    }
}
