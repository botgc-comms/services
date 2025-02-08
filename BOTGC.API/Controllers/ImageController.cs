using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Services.Interfaces;
using Servicesx.Controllers;

namespace Services.Controllers
{
    [ApiController]
    [Route("api/images")]
    public class ImagesController : ControllerBase
    {
        private const string __CACHE_WINNERIMAGE = "Winner-Image-{Id}";

        private readonly IImageServices _imageService;
        private readonly ITrophyService _trophyService;
        private readonly ICacheService _cacheService;

        private readonly ILogger<ImagesController> _logger;
        private readonly AppSettings _settings;

        /// <summary>
        /// Initializes a new instance of the <see cref="MembersController"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="imageService">Service handling image processing.</param>
        /// <param name="trophyService">Service handling trophy data retrieval.</param>
        public ImagesController(ILogger<ImagesController> logger,
                                IOptions<AppSettings> settings,
                                IImageServices imageService,
                                ITrophyService trophyService,
                                ICacheService cacheService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _imageService = imageService ?? throw new ArgumentNullException(nameof(imageService));
            _trophyService = trophyService ?? throw new ArgumentNullException(nameof(trophyService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        /// <summary>
        /// Retrieves the processed winner image for a given trophy ID.
        /// Supports 'Cache-Control: no-cache' to bypass cached results.
        /// </summary>
        /// <param name="id">The trophy ID.</param>
        /// <returns>The processed winner image.</returns>
        /// <response code="200">Returns the processed winner image.</response>
        /// <response code="404">If no winner image is found.</response>
        /// <response code="500">If an internal server error occurs.</response>
        /// <remarks>
        /// Clients can send the **'Cache-Control: no-cache'** header to force a fresh image processing.
        /// </remarks>
        [HttpGet("winners/{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [Produces("image/jpeg")]
        public async Task<IActionResult> GetWinnerImage(string id)
        {
            id = id.ToUpper();
            var cacheKey = __CACHE_WINNERIMAGE.Replace("{id}", id);

            _logger.LogInformation("Fetching winner image for trophy ID: {Id}", id);

            // Check if the processed image is in cache
            var cachedImage = await _cacheService.GetAsync<byte[]>(cacheKey);
            if (cachedImage != null)
            {
                _logger.LogInformation("Returning cached winner image for trophy ID: {Id}", id);
                return File(new MemoryStream(cachedImage), "image/jpeg");
            }

            // Retrieve the original winner image
            var imageStream = await _trophyService.GetWinnerImageByTrophyIdAsync(id);
            if (imageStream == null)
            {
                _logger.LogWarning("No winner image found for trophy ID: {Id}", id);
                return NotFound($"No winner image found for trophy ID '{id}'.");
            }

            // Process the image (crop and center faces)
            _logger.LogInformation("Processing winner image...");
            var processedImageStream = await _imageService.CropAndCentreFacesAsync(imageStream);
            if (processedImageStream == null)
            {
                _logger.LogError("Failed to process winner image for trophy ID: {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to process winner image.");
            }

            // Store the processed image in cache
            var memoryStream = new MemoryStream();
            await processedImageStream.CopyToAsync(memoryStream);
            var processedImageBytes = memoryStream.ToArray();

            await _cacheService.SetAsync(cacheKey, processedImageBytes, TimeSpan.FromMinutes(_settings.Cache.TTL_mins));
            _logger.LogInformation("Cached winner image for trophy ID: {Id}", id);

            // Return the processed image
            return File(new MemoryStream(processedImageBytes), "image/jpeg");
        }
    }
}
