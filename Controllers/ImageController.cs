using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Servicesx.Controllers;

namespace Services.Controllers
{
    [ApiController]
    [Route("api/images")]
    public class ImagesController : ControllerBase
    {
        private readonly IImageServices _imageService;
        private readonly ITrophyService _trophyService;
        private readonly ILogger<ImagesController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrophiesController"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="imageService">Service handling image processing.</param>
        /// <param name="trophyService">Service handling trophy data retrieval.</param>
        public ImagesController(ILogger<ImagesController> logger, IImageServices imageService, ITrophyService trophyService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
            _trophyService = trophyService ?? throw new ArgumentNullException(nameof(trophyService));
        }

        /// <summary>
        /// Retrieves the processed winner image for a given trophy ID.
        /// </summary>
        [HttpGet("winners/{id}")]
        public async Task<IActionResult> GetWinnerImage(string id)
        {
            _logger.LogInformation("Fetching winner image for trophy ID: {Id}", id);

            var imageStream = await _trophyService.GetWinnerImageByTrophyIdAsync(id);
            if (imageStream == null)
            {
                return NotFound($"No winner image found for trophy ID '{id}'.");
            }

            _logger.LogInformation("Processing winner image...");
            var processedImageStream = await _imageService.CropAndCentreFacesAsync(imageStream);
            if (processedImageStream == null)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to process winner image.");
            }

            return File(processedImageStream, "image/jpeg"); // Automatically detects MIME type.
        }
    }
}
