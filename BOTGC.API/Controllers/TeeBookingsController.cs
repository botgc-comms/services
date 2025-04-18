﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace BOTGC.API.Controllers
{
    /// <summary>
    /// Controller for handling golf round-related operations.
    /// </summary>
    [ApiController]
    [Route("api/teesheets")]
    [Produces("application/json")]
    public class TeesheetController : ControllerBase
    {
        private readonly IDataService _reportService;
        private readonly ILogger<TeesheetController> _logger;
        private readonly ITeeTimeUsageTaskQueue _taskQueue;

        /// <summary>
        /// Initializes a new instance of the <see cref="RoundsController"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="reportService">Service for handling round reports.</param>
        public TeesheetController(ILogger<TeesheetController> logger, IDataService reportService, ITeeTimeUsageTaskQueue taskQueue)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _reportService = reportService ?? throw new ArgumentNullException(nameof(reportService));
            _taskQueue = taskQueue ?? throw new ArgumentNullException(nameof(taskQueue));
        }

        /// <summary>
        /// Retrieves the tee sheet for the given date
        /// </summary>
        /// <param name="Date">The date of the tee sheet</param>
        /// <returns>The Tee sheet including all the tee times that are booked and the players that booked them.</returns>
        /// <response code="200">Returns the teesheet for the given date.</response>
        /// <response code="204">No teesheet found for the given date.</response>
        /// <response code="500">An internal server error occurred.</response>
        [HttpGet("{date}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(ScorecardDto))]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TeeSheetDto>> GetTeeSheetByDate(DateTime date)
        {
            _logger.LogInformation("Fetching tee bookings on {date}...", date.ToString("dd MM yyyy"));

            try
            {
                var teesheet = await _reportService.GetTeeSheetByDateAsync(date);

                if (teesheet == null)
                {
                    _logger.LogWarning("No teesheet was found for the date {date}.", date.ToString("dd MM yyyy"));
                    return NoContent();
                }

                _logger.LogInformation("Successfully retrieved teesheet for date {date}.", date.ToString("dd MM yyyy"));
                return Ok(teesheet);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving teesheet for date {date}.", date.ToString("dd MM yyyy"));
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the teesheet.");
            }
        }

        /// <summary>
        /// Retrieves the tee sheet for the given date
        /// </summary>
        /// <param name="From">The date of the tee sheet</param>
        /// <returns>The Tee sheet including all the tee times that are booked and the players that booked them.</returns>
        /// <response code="200">Returns the teesheet for the given date.</response>
        /// <response code="204">No teesheet found for the given date.</response>
        /// <response code="500">An internal server error occurred.</response>
        [HttpPost("teeUsage/prepare")]
        public async Task<IActionResult> PrepareTeetimeUsage([FromBody] DateRange dateRange)
        {
            var taskItem = new TeeTimeUsageTaskItem
            {
                FromDate = dateRange.Start,
                ToDate = dateRange.End,
            };

            await _taskQueue.QueueTaskAsync(taskItem);

            return Accepted(new { Message = "Competition status retrieval started." });
        }
    }
}
