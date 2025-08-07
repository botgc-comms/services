﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services;
using System.Runtime;
using System.Threading;
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
    [Route("api/eventLog")]
    [Produces("application/json")]
    public class EventLogController(IOptions<AppSettings> settings,
                                ILogger<CompetitionsController> logger,
                                IMediator mediator,
                                IServiceScopeFactory serviceScopeFactory) : Controller
    {
        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        private readonly ILogger<CompetitionsController> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        [HttpGet("mobileOrders")]
        public async Task<IActionResult> GetMobileOrders([FromQuery] DateTime? forDate = null)
        {
            _logger.LogInformation($"Fetching mobile orders...");

            try
            {
                var query = new GetMobileOrdersForDateQuery() { ForDate = forDate };
                var mobileOrders = await _mediator.Send(query, HttpContext.RequestAborted);

                if (mobileOrders != null && mobileOrders.Any())
                {
                    _logger.LogInformation($"Successfully retrieved {mobileOrders.Count} mobile orders.");
                }

                return Ok(mobileOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving mobile orders.");
                return StatusCode(StatusCodes.Status500InternalServerError, $"An error occurred while retrieving mobile orders.");
            }
        }

    }
}
