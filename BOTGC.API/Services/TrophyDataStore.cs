using Microsoft.Extensions.Logging;
using Services.Interfaces;
using Services.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Services.Services
{
    /// <summary>
    /// Data store for managing trophy data.
    /// </summary>
    public class TrophyDataStore : ITrophyDataStore
    {
        private readonly TrophyFilesDiskStorage _trophyFiles;
        private readonly ILogger<TrophyDataStore> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrophyDataStore"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="trophyFiles">File-based storage for trophy metadata.</param>
        public TrophyDataStore(ILogger<TrophyDataStore> logger, TrophyFilesDiskStorage trophyFiles)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _trophyFiles = trophyFiles ?? throw new ArgumentNullException(nameof(trophyFiles));
        }

        /// <summary>
        /// Retrieves a list of trophy IDs from storage.
        /// </summary>
        /// <returns>A read-only collection of trophy IDs.</returns>
        public async Task<IReadOnlyCollection<string>> ListTrophyIdsAsync()
        {
            try
            {
                _logger.LogInformation("Fetching trophy list from files...");

                var trophies = await _trophyFiles.ListTrophiesAsync();
                if (!trophies.Any())
                {
                    _logger.LogWarning("No trophies found in the data store.");
                    return Array.Empty<string>();
                }

                var trophyIds = trophies
                    .OrderBy(t => t.Slug, StringComparer.OrdinalIgnoreCase)
                    .Select(t => t.Slug)
                    .ToArray();

                _logger.LogInformation("Successfully retrieved {Count} trophies.", trophyIds.Length);
                return trophyIds;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving trophy IDs.");
                throw;
            }
        }

        /// <summary>
        /// Retrieves trophy metadata by its unique ID.
        /// </summary>
        /// <param name="id">The unique slug of the trophy.</param>
        /// <returns>The trophy metadata if found; otherwise, <c>null</c>.</returns>
        public async Task<TrophyMetadata?> GetTrophyByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Attempted to retrieve trophy with an invalid ID.");
                throw new ArgumentException("Trophy ID cannot be null or empty.", nameof(id));
            }

            _logger.LogInformation("Fetching trophy metadata for ID: {Id}", id);

            try
            {
                var trophy = await _trophyFiles.GetTrophyByIdAsync(id);
                if (trophy == null)
                {
                    _logger.LogWarning("No trophy metadata found for ID: {Id}", id);
                    return null;
                }

                _logger.LogInformation("Successfully retrieved trophy metadata for ID: {Id}", id);
                return trophy;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving trophy metadata for ID: {Id}", id);
                throw;
            }
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
                _logger.LogWarning("Attempted to retrieve winner image with an invalid trophy ID.");
                throw new ArgumentException("Trophy ID cannot be null or empty.", nameof(id));
            }

            _logger.LogInformation("Fetching winner image for trophy ID: {Id}", id);

            try
            {
                var winnerImageStream = await _trophyFiles.GetWinnerImageByTrophyIdAsync(id);
                if (winnerImageStream == null)
                {
                    _logger.LogWarning("No winner image found for trophy ID: {Id}", id);
                    return null;
                }

                _logger.LogInformation("Successfully retrieved winner image for trophy ID: {Id}", id);
                return winnerImageStream;
            }
            catch (FileNotFoundException)
            {
                _logger.LogWarning("Winner image file not found for trophy ID: {Id}", id);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving winner image for trophy ID: {Id}", id);
                throw;
            }
        }
    }
}
