using System.Text.Json;
using BOTGC.API.Common;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.EventBus;
using BOTGC.API.Services.EventBus.Detectors;
using BOTGC.API.Services.EventBus.Events;
using BOTGC.API.Services.Events;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Controllers;

[ApiController]
[Route("api/events")]
[Produces("application/json")]
public sealed class EventsController(
    IOptions<AppSettings> settings,
    ILogger<EventsController> logger,
    IMediator mediator) : ControllerBase
{
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILogger<EventsController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    [HttpGet("mobileOrders")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetMobileOrders([FromQuery] DateTime? forDate = null)
    {
        _logger.LogInformation("Fetching mobile orders...");

        try
        {
            var query = new GetMobileOrdersForDateQuery { ForDate = forDate };
            var mobileOrders = await _mediator.Send(query, HttpContext.RequestAborted);

            if (mobileOrders != null && mobileOrders.Any())
            {
                _logger.LogInformation("Successfully retrieved {Count} mobile orders.", mobileOrders.Count);
            }

            return Ok(mobileOrders);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving mobile orders.");
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving mobile orders.");
        }
    }

    [HttpGet("members/{memberId:int}/junior-progress/latest")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(JuniorProgressChangedEvent))]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<JuniorProgressChangedEvent>> GetLatestJuniorProgressEvent(int memberId, [FromQuery] int? take = null)
    {
        if (memberId <= 0)
        {
            return BadRequest("memberId must be a positive integer.");
        }

        var readTake = take.GetValueOrDefault(250);
        if (readTake <= 0) readTake = 250;
        if (readTake > 2000) readTake = 2000;

        try
        {
            var evt = await _mediator.Send(new GetLatestJuniorProgressEventQuery(memberId, readTake), HttpContext.RequestAborted);

            if (evt is null)
            {
                return NoContent();
            }

            return Ok(evt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest junior progress event for member {MemberId}.", memberId);
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the latest junior progress event.");
        }
    }

    [HttpPost("detectors/trigger")]
    [ProducesResponseType(StatusCodes.Status202Accepted)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult TriggerDetector(
       [FromServices] IMemberDetectorTriggerScheduler triggerScheduler,
       [FromServices] IDetectorNameRegistry detectorRegistry,
       [FromBody] TriggerMemberDetectorRequest request)
    {
        if (request is null) return BadRequest("Request body is required.");
        if (string.IsNullOrWhiteSpace(request.DetectorName)) return BadRequest("DetectorName is required.");
        if (request.MemberId <= 0) return BadRequest("MemberId must be a positive integer.");

        var detectorName = request.DetectorName.Trim();

        if (!detectorRegistry.IsValid(detectorName))
        {
            return BadRequest(new
            {
                message = $"Unknown detectorName '{detectorName}'.",
                availableDetectors = detectorRegistry.GetAll().OrderBy(x => x).ToArray(),
            });
        }

        var quietSeconds = request.QuietPeriodSeconds.GetValueOrDefault(5);
        if (quietSeconds <= 0) quietSeconds = 5;
        if (quietSeconds > 300) quietSeconds = 300;

        var correlationId = string.IsNullOrWhiteSpace(request.CorrelationId)
            ? Guid.NewGuid().ToString("N")
            : request.CorrelationId.Trim();

        triggerScheduler.Schedule(
            detectorName: request.DetectorName.Trim(),
            memberId: request.MemberId,
            correlationId: correlationId,
            quietPeriod: TimeSpan.FromSeconds(quietSeconds),
            stoppingToken: HttpContext.RequestAborted);

        return Accepted(new
        {
            detectorName = request.DetectorName.Trim(),
            memberId = request.MemberId,
            correlationId,
            quietPeriodSeconds = quietSeconds,
        });
    }
}

public sealed class TriggerMemberDetectorRequest
{
    public required string DetectorName { get; init; }
    public required int MemberId { get; init; }
    public string? CorrelationId { get; init; }
    public int? QuietPeriodSeconds { get; init; }
}


