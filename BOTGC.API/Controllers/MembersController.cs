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
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(MembershipReportDto))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<MembershipReportDto>> GetMembershipReport([FromQuery] string? financialYear)
        {
            _logger.LogInformation("Fetching membership report");

            try
            {
                var today = DateTime.UtcNow.Date;
                var currentFyStartYear = today.Month >= 10 ? today.Year : today.Year - 1;
                var currentFyEndYear = currentFyStartYear + 1;

                if (string.IsNullOrWhiteSpace(financialYear))
                {
                    financialYear = ToFinancialYearString(currentFyStartYear, currentFyEndYear);
                }

                if (!TryParseFinancialYear(financialYear, out var fyStartYear, out var fyEndYear))
                {
                    return BadRequest("financialYear must be in the format '24/25' and represent consecutive years.");
                }

                var fyStart = new DateTime(fyStartYear, 10, 1);
                var fyEnd = new DateTime(fyEndYear, 9, 30);

                var currentFyStart = new DateTime(currentFyStartYear, 10, 1);
                var currentFyEnd = currentFyStart.AddYears(1).AddDays(-1);

                var isCurrentFy = fyStart == currentFyStart && fyEnd == currentFyEnd;

                var anchorDate = isCurrentFy ? today : fyEnd;

                var membershipReport = await _membershipReporting.GetManagementReport(anchorDate, HttpContext.RequestAborted);

                if (membershipReport == null)
                {
                    _logger.LogWarning("Failed to generate management report.");
                    return NoContent();
                }

                membershipReport.Links.Clear();

                var selfFy = ToFinancialYearString(fyStartYear, fyEndYear);
                membershipReport.Links.Add(new HateoasLink
                {
                    Rel = "self",
                    Href = Url.ActionLink(
                        action: nameof(GetMembershipReport),
                        controller: "Members",
                        values: new { financialYear = selfFy }) ?? string.Empty,
                    Method = "GET"
                });

                var prevStartYear = fyStartYear - 1;
                var prevEndYear = fyEndYear - 1;
                var prevFy = ToFinancialYearString(prevStartYear, prevEndYear);

                membershipReport.Links.Add(new HateoasLink
                {
                    Rel = "previous",
                    Href = Url.ActionLink(
                        action: nameof(GetMembershipReport),
                        controller: "Members",
                        values: new { financialYear = prevFy }) ?? string.Empty,
                    Method = "GET"
                });

                if (!isCurrentFy && fyEnd < currentFyEnd)
                {
                    var nextStartYear = fyStartYear + 1;
                    var nextEndYear = fyEndYear + 1;
                    var nextFy = ToFinancialYearString(nextStartYear, nextEndYear);

                    membershipReport.Links.Add(new HateoasLink
                    {
                        Rel = "next",
                        Href = Url.ActionLink(
                            action: nameof(GetMembershipReport),
                            controller: "Members",
                            values: new { financialYear = nextFy }) ?? string.Empty,
                        Method = "GET"
                    });
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

        private static bool TryParseFinancialYear(string input, out int startYear, out int endYear)
        {
            startYear = 0;
            endYear = 0;

            if (string.IsNullOrWhiteSpace(input)) return false;

            input = input.Trim();

            if (input.Length != 5) return false;
            if (input[2] != '/') return false;

            var startPart = input.Substring(0, 2);
            var endPart = input.Substring(3, 2);

            if (!int.TryParse(startPart, out var startYY)) return false;
            if (!int.TryParse(endPart, out var endYY)) return false;

            if (((startYY + 1) % 100) != endYY) return false;

            startYear = 2000 + startYY;
            endYear = 2000 + endYY;

            if (startYear < 2000 || startYear > 2099) return false;
            if (endYear != startYear + 1) return false;

            return true;
        }

        private static string ToFinancialYearString(int startYear, int endYear)
        {
            var sy = startYear % 100;
            var ey = endYear % 100;
            return $"{sy:00}/{ey:00}";
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
        /// Retrieves a list of all lady members.
        /// </summary>
        /// <returns>A list of lady members with their details.</returns>
        /// <response code="200">Returns the list of lady members.</response>
        /// <response code="204">No junior members found.</response>
        /// <response code="500">An internal server error occurred.</response>
        [HttpGet("ladies")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<MemberDto>))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyCollection<MemberDto>>> GetLadyMembers()
        {
            _logger.LogInformation("Fetching lady members...");

            try
            {
                var query = new GetLadyMembersQuery();
                var ladyMembers = await _mediator.Send(query, HttpContext.RequestAborted);

                if (ladyMembers == null || ladyMembers.Count == 0)
                {
                    _logger.LogWarning("No lady members found.");
                    return NoContent();
                }

                _logger.LogInformation("Successfully retrieved {Count} lady members.", ladyMembers.Count);
                return Ok(ladyMembers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving lady members.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving lady members.");
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
        /// Retrieves a list of all members on a waiting list
        /// </summary>
        /// <returns>A list of waiting members with their details.</returns>
        /// <response code="200">Returns the list of current members.</response>
        /// <response code="204">No current members found.</response>
        /// <response code="500">An internal server error occurred.</response>
        [HttpGet("waiting")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<MemberDto>))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyCollection<MemberDto>>> GetWaitingMembers()
        {
            _logger.LogInformation("Fetching waiting members...");

            try
            {
                var query = new GetWaitingMembersQuery();
                var waitingMembers = await _mediator.Send(query, HttpContext.RequestAborted);

                if (waitingMembers == null || waitingMembers.Count == 0)
                {
                    _logger.LogWarning("No waiting members found.");
                    return NoContent();
                }

                _logger.LogInformation("Successfully retrieved {Count} waiting members.", waitingMembers.Count);
                return Ok(waitingMembers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving waiting members.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving waiting members.");
            }
        }

        /// <summary>
        /// Retrieves a list of all members on a waiting list
        /// </summary>
        /// <returns>A list of waiting members with their details.</returns>
        /// <response code="200">Returns the list of current members.</response>
        /// <response code="204">No current members found.</response>
        /// <response code="500">An internal server error occurred.</response>
        [HttpGet("new")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<MemberDetailsDto>))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IReadOnlyCollection<MemberDetailsDto>>> GetNewMembers()
        {
            _logger.LogInformation("Fetching new members...");

            try
            {
                var query = new GetNewMembersQuery();
                var newMembers = await _mediator.Send(query, HttpContext.RequestAborted);

                if (newMembers == null || newMembers.Count == 0)
                {
                    _logger.LogWarning("No new members found.");
                    return NoContent();
                }

                _logger.LogInformation("Successfully retrieved {Count} new members.", newMembers.Count);
                return Ok(newMembers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving new members.");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving new members.");
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
                _logger.LogError(ex, $"Error retrieving rounds for member {memberId}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while retrieving rounds for member {memberId}.");
            }
        }

        /// <summary>
        /// Retrieves the handicap history for a specific member.
        /// </summary>
        /// <returns> Returns the handicap history for the specified member </returns>
        /// <response code="200">Returns the list of junior members.</response>
        /// <response code="204">No junior members found.</response>
        /// <response code="500">An internal server error occurred.</response>
        [HttpGet("{memberId}/handicapHistory")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(PlayerHandicapSummaryDto))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PlayerHandicapSummaryDto>> GetHandicapHisotryByMember(int memberId, DateTime? fromDate, DateTime? toDate)
        {
            _logger.LogInformation($"Fetching handicap history for member {memberId}...");

            try
            {
                var query = new GetHandicapHistoryByMemberQuery() { MemberId = memberId };
                if (fromDate.HasValue) query.FromDate = fromDate.Value;
                if (toDate.HasValue) query.ToDate = toDate.Value;

                var handicapHistory = await _mediator.Send(query, HttpContext.RequestAborted);

                if (handicapHistory == null || handicapHistory.HandicapIndexPoints == null || !handicapHistory.HandicapIndexPoints.Any())
                {
                    _logger.LogWarning($"No handicap history was found for member {memberId}.");
                    return NoContent();
                }

                _logger.LogInformation($"Successfully retrieved {handicapHistory.HandicapIndexPoints.Count} handicap entries for member {memberId}.");
                return Ok(handicapHistory);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "No player found for member ID {MemberId}", memberId);
                return NotFound(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving handicap history for member {memberId}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while retrieving rounds for member {memberId}.");
            }
        }

        /// <summary>
        /// Retrieve subscription payment details
        /// </summary>
        /// <returns>Retrieve subscription payment details</returns>
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

        [HttpGet("{memberId}/competitionResults")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<PlayerCompetitionResultsDto>))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCompetitionResultsByPlayer(string memberId, DateTime? fromDate, DateTime? toDate, int? maxFinishingPosition)
        {
            _logger.LogInformation($"Fetching competition results for player {memberId}...");

            try
            {
                var query = new GetCompetitionResultsByMemberIdQuery() { MemberId = memberId };

                if (fromDate.HasValue) query.FromDate = fromDate.Value; 
                if (toDate.HasValue) query.ToDate = toDate.Value;
                if (maxFinishingPosition.HasValue) query.MaxFinishingPosition = maxFinishingPosition.Value;

                var playerResults = await _mediator.Send(query, HttpContext.RequestAborted);

                if (playerResults == null)
                {
                    _logger.LogWarning($"No competition results found for member {memberId}.");
                    return NoContent();
                }

                _logger.LogInformation($"Successfully retrieved competition results for member {memberId}.");
                return Ok(playerResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving competition results for member id {memberId}.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while retrieving competition results for member {memberId}.");
            }
        }

        [HttpGet("juniors/competitionResults")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<PlayerCompetitionResultsDto>))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCompetitionResultsForJuniors(DateTime? fromDate, DateTime? toDate, int? maxFinishingPosition)
        {
            _logger.LogInformation($"Fetching competition results for all juniors...");

            try
            {
                var query = new GetCompetitionResultsForJuniorsQuery();

                if (fromDate.HasValue) query.FromDate = fromDate.Value;
                if (toDate.HasValue) query.ToDate = toDate.Value;
                if (maxFinishingPosition.HasValue) query.MaxFinishingPosition = maxFinishingPosition.Value;

                var playerResults = await _mediator.Send(query, HttpContext.RequestAborted);

                if (playerResults == null)
                {
                    _logger.LogWarning($"No competition results found for member juniors.");
                    return NoContent();
                }

                _logger.LogInformation($"Successfully retrieved competition results for {playerResults.Count} Juniors.");
                return Ok(playerResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving competition results for all juniors.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while retrieving competition results for all juniors.");
            }
        }

        [HttpGet("ladies/competitionResults")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<PlayerCompetitionResultsDto>))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetCompetitionResultsForLadies(DateTime? fromDate, DateTime? toDate, int? maxFinishingPosition)
        {
            _logger.LogInformation($"Fetching competition results for all ladies...");

            try
            {
                var query = new GetCompetitionResultsForLadiesQuery();

                if (fromDate.HasValue) query.FromDate = fromDate.Value;
                if (toDate.HasValue) query.ToDate = toDate.Value;
                if (maxFinishingPosition.HasValue) query.MaxFinishingPosition = maxFinishingPosition.Value;

                var playerResults = await _mediator.Send(query, HttpContext.RequestAborted);

                if (playerResults == null)
                {
                    _logger.LogWarning($"No competition results found for ladies.");
                    return NoContent();
                }

                _logger.LogInformation($"Successfully retrieved competition results for {playerResults.Count} ladies.");
                return Ok(playerResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving competition results for all ladies.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while retrieving competition results for all ladies.");
            }
        }


        [HttpGet("juniors/handicaps")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<PlayerHandicapSummaryDto>))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetHandicapDataForJuniors()
        {
            _logger.LogInformation($"Fetching competition results for all juniors...");

            try
            {
                var query = new GetHandicapSummaryForJuniorsQuery();
                
                var playerResults = await _mediator.Send(query, HttpContext.RequestAborted);

                if (playerResults == null)
                {
                    _logger.LogWarning($"No competition results found for member juniors.");
                    return NoContent();
                }

                _logger.LogInformation($"Successfully retrieved competition results for {playerResults.Count} Juniors.");
                return Ok(playerResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving competition results for all juniors.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while retrieving competition results for all juniors.");
            }
        }

        [HttpGet("ladies/handicaps")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<PlayerHandicapSummaryDto>))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetHandicapDataForLadies()
        {
            _logger.LogInformation($"Fetching handicap data for all ladies...");

            try
            {
                var query = new GetHandicapSummaryForLadiesQuery();

                var playerResults = await _mediator.Send(query, HttpContext.RequestAborted);

                if (playerResults == null)
                {
                    _logger.LogWarning($"No handicap data found for ladies.");
                    return NoContent();
                }

                _logger.LogInformation($"Successfully retrieved handicap data for {playerResults.Count} ladies.");
                return Ok(playerResults);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving handicap data for all ladies.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while retrieving handicap data for all ladies.");
            }
        }

        //[HttpGet("quiz/attempts/{memberId}")]
        //[ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IReadOnlyCollection<CompletedQuizAttemptDto>))]
        //[ProducesResponseType(StatusCodes.Status204NoContent)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public async Task<IActionResult> GetCompletedQuizAttempts(int memberId)
        //{
        //    _logger.LogInformation($"Fetching completed Quiz attempts for member...");

        //    try
        //    {
        //        var query = new GetCompletedQuizAttemptsQuery(memberId);

        //        var playerResults = await _mediator.Send(query, HttpContext.RequestAborted);

        //        if (playerResults == null)
        //        {
        //            _logger.LogWarning($"No attempted quizes found.");
        //            return NoContent();
        //        }

        //        _logger.LogInformation($"Successfully retrieved {playerResults.Count} quiz attempts for member {memberId}.");
        //        return Ok(playerResults);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"Error retrieving attempted quiz data for member {memberId}.");
        //        return StatusCode(StatusCodes.Status500InternalServerError, $"Error retrieving attempted quiz data for member {memberId}.");
        //    }
        //}
    }
}
