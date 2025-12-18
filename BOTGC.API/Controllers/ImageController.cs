using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using BOTGC.API.Interfaces;
using Servicesx.Controllers;

namespace BOTGC.API.Controllers
{
    [ApiController]
    [Route("api/images")]
    public class ImagesController : ControllerBase
    {
        private const string __CACHE_WINNERIMAGE = "Winner-Image-{id}";

        private readonly IImageServices _imageService;
        private readonly ITrophyService _trophyService;
        private readonly ICacheService _cacheService;

        private readonly ILogger<ImagesController> _logger;
        private readonly AppSettings _settings;

        /// <summary>
        /// Creates a new <see cref="ImagesController"/>.
        /// </summary>
        /// <param name="logger">Application logger.</param>
        /// <param name="settings">Application settings.</param>
        /// <param name="imageService">Service responsible for image processing.</param>
        /// <param name="trophyService">Service responsible for retrieving trophy data.</param>
        /// <param name="cacheService">Service responsible for caching.</param>
        public ImagesController(
            ILogger<ImagesController> logger,
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
        /// Gets the processed winner image for a trophy.
        /// </summary>
        /// <param name="id">The trophy identifier.</param>
        /// <returns>A JPEG image.</returns>
        /// <remarks>
        /// The response is cached. To bypass caching, send the request header <c>Cache-Control: no-cache</c>.
        /// </remarks>
        /// <response code="200">The processed winner image was returned.</response>
        /// <response code="404">No winner image exists for the supplied trophy identifier.</response>
        /// <response code="500">The winner image could not be processed.</response>
        [HttpGet("winners/{id}")]
        [Produces("image/jpeg")]
        [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetWinnerImage([FromRoute] string id)
        {
            id = id.ToUpperInvariant();
            var cacheKey = __CACHE_WINNERIMAGE.Replace("{id}", id);

            _logger.LogInformation("Fetching winner image for trophy ID: {Id}", id);

            var cachedImage = await _cacheService.GetAsync<byte[]>(cacheKey);
            if (cachedImage != null)
            {
                _logger.LogInformation("Returning cached winner image for trophy ID: {Id}", id);
                return File(new MemoryStream(cachedImage), "image/jpeg");
            }

            var imageStream = await _trophyService.GetWinnerImageByTrophyIdAsync(id);
            if (imageStream == null)
            {
                _logger.LogWarning("No winner image found for trophy ID: {Id}", id);
                return NotFound($"No winner image found for trophy ID '{id}'.");
            }

            _logger.LogInformation("Processing winner image...");
            var processedImageStream = await _imageService.CropAndCentreFacesAsync(imageStream);
            if (processedImageStream == null)
            {
                _logger.LogError("Failed to process winner image for trophy ID: {Id}", id);
                return StatusCode(StatusCodes.Status500InternalServerError, "Failed to process winner image.");
            }

            var memoryStream = new MemoryStream();
            await processedImageStream.CopyToAsync(memoryStream);
            var processedImageBytes = memoryStream.ToArray();

            await _cacheService.SetAsync(
                cacheKey,
                processedImageBytes,
                TimeSpan.FromMinutes(_settings.Cache.LongTerm_TTL_mins));

            _logger.LogInformation("Cached winner image for trophy ID: {Id}", id);

            return File(new MemoryStream(processedImageBytes), "image/jpeg");
        }
    }
}
