using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Interfaces;
using Services.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Services.Services
{
    /// <summary>
    /// Handles reading trophy metadata from disk storage.
    /// </summary>
    public class TrophyFilesDiskStorage : ITrophyFiles
    {
        private const string __CACHE_TROPHYFILES = "Trophy_files";

        private readonly AppSettings _settings;
        private readonly ILogger<TrophyFilesDiskStorage> _logger;
        private IServiceScopeFactory _serviceScopeFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrophyFilesDiskStorage"/> class.
        /// </summary>
        /// <param name="settings">Application settings.</param>
        /// <param name="logger">Logger instance.</param>
        public TrophyFilesDiskStorage(IOptions<AppSettings> settings, ILogger<TrophyFilesDiskStorage> logger, IServiceScopeFactory serviceScopeFactory)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
        }

        /// <summary>
        /// Retrieves a list of all trophies by reading metadata from files.
        /// </summary>
        /// <returns>A collection of <see cref="TrophyMetadata"/>.</returns>
        public async Task<IReadOnlyCollection<TrophyMetadata>> ListTrophiesAsync()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            // Attempt to retrieve from cache
            var cachedTrophies = await cacheService.GetAsync<IReadOnlyCollection<TrophyMetadata>>(__CACHE_TROPHYFILES).ConfigureAwait(false);
            if (cachedTrophies != null && cachedTrophies.Any())
            {
                _logger.LogInformation("Trophy data retrieved from cache.");
                return cachedTrophies;
            }

            // Validate trophy storage path
            var trophiesPath = _settings.TrophyFilePath;
            if (string.IsNullOrWhiteSpace(trophiesPath))
            {
                _logger.LogError("Trophy file path is not configured.");
                return Array.Empty<TrophyMetadata>();
            }

            if (!Directory.Exists(trophiesPath))
            {
                _logger.LogWarning("Trophy directory does not exist: {Path}", trophiesPath);
                return Array.Empty<TrophyMetadata>();
            }

            // Read metadata from all directories
            var trophies = new List<TrophyMetadata>();
            try
            {
                var directories = Directory.GetDirectories(trophiesPath);
                foreach (var dir in directories)
                {
                    var metadataPath = Path.Combine(dir, "metadata.json");

                    if (!File.Exists(metadataPath))
                    {
                        _logger.LogWarning("Metadata file missing in directory: {Dir}", dir);
                        continue;
                    }

                    try
                    {
                        var json = await File.ReadAllTextAsync(metadataPath).ConfigureAwait(false);
                        if (string.IsNullOrWhiteSpace(json))
                        {
                            _logger.LogWarning("Metadata file is empty: {MetadataPath}", metadataPath);
                            continue;
                        }

                        var metadata = JsonSerializer.Deserialize<TrophyMetadata>(json, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (metadata == null)
                        {
                            _logger.LogWarning("Metadata file is invalid: {MetadataPath}", metadataPath);
                            continue;
                        }

                        metadata.ImageUrl = ResolvePath(dir, metadata.ImageUrl);
                        metadata.WinnerImage = ResolvePath(dir, metadata.WinnerImage);

                        trophies.Add(metadata);
                    }
                    catch (JsonException ex)
                    {
                        _logger.LogError(ex, "Invalid metadata.json format in {Dir}", dir);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Unexpected error while reading metadata.json in {Dir}", dir);
                    }
                }

                // Store the result in cache
                if (trophies.Any())
                {
                    await cacheService.SetAsync(__CACHE_TROPHYFILES, trophies, TimeSpan.FromMinutes(_settings.Cache.TTL_mins)).ConfigureAwait(false);
                }

                _logger.LogInformation("Successfully loaded {Count} trophies.", trophies.Count);
                return trophies;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load trophies from directory: {Path}", trophiesPath);
                return Array.Empty<TrophyMetadata>();
            }
        }

        /// <summary>
        /// Retrieves trophy metadata by its unique ID.
        /// </summary>
        /// <param name="id">The unique trophy slug.</param>
        /// <returns>The <see cref="TrophyMetadata"/> if found; otherwise, null.</returns>
        public async Task<TrophyMetadata?> GetTrophyByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Attempted to retrieve trophy with an empty ID.");
                return null;
            }

            var trophies = await ListTrophiesAsync();
            var trophy = trophies.SingleOrDefault(t => string.Equals(t.Slug, id, StringComparison.OrdinalIgnoreCase));

            if (trophy == null)
            {
                _logger.LogWarning("Trophy with ID {Id} not found.", id);
                return null;
            }

            return trophy;
        }

        /// <summary>
        /// Resolves a relative path to an absolute URL.
        /// </summary>
        /// <param name="directory">The trophy's directory path.</param>
        /// <param name="relativePath">The relative path of the file.</param>
        /// <returns>The resolved absolute URL.</returns>
        private static string? ResolvePath(string directory, string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return null;
            }

            var absolutePath = Path.Combine(directory, relativePath);
            return Path.GetFullPath(absolutePath).Replace("\\", "/");
        }

        /// <summary>
        /// Retrieves the winner image for a given trophy ID.
        /// </summary>
        /// <param name="id">The unique trophy slug.</param>
        /// <returns>The winner image as a <see cref="Stream"/> if found; otherwise, null.</returns>
        public async Task<Stream?> GetWinnerImageByTrophyIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Attempted to retrieve winner image with an empty trophy ID.");
                return null;
            }

            var trophy = await GetTrophyByIdAsync(id);
            if (trophy == null)
            {
                _logger.LogWarning("Trophy with ID {Id} not found.", id);
                return null;
            }

            if (string.IsNullOrEmpty(trophy.WinnerImage))
            {
                _logger.LogWarning("Trophy with ID {Id} has no winner image specified.", id);
                return null;
            }

            var imagePath = trophy.WinnerImage;
            if (!File.Exists(imagePath))
            {
                _logger.LogWarning("Winner image file does not exist: {ImagePath}", imagePath);
                return null;
            }

            try
            {
                _logger.LogInformation("Loading winner image for trophy ID: {Id}", id);

                // Open file as stream
                var memoryStream = new MemoryStream();
                using (var fileStream = File.OpenRead(imagePath))
                {
                    await fileStream.CopyToAsync(memoryStream);
                }

                memoryStream.Seek(0, SeekOrigin.Begin);
                return memoryStream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load winner image for trophy ID: {Id}", id);
                return null;
            }
        }
    }
}
