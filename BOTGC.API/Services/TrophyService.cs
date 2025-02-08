using Services.Interfaces;
using Services.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Services.Services
{
    /// <summary>
    /// Service for handling trophy data retrieval and processing.
    /// </summary>
    public class TrophyService : ITrophyService
    {
        private readonly ITrophyDataStore _trophyDataStore;
        private readonly ILogger<TrophyService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="TrophyService"/> class.
        /// </summary>
        /// <param name="logger">Logger instance.</param>
        /// <param name="trophyDataStore">Data store for retrieving trophies.</param>
        public TrophyService(ILogger<TrophyService> logger, ITrophyDataStore trophyDataStore)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _trophyDataStore = trophyDataStore ?? throw new ArgumentNullException(nameof(trophyDataStore));
        }

        /// <summary>
        /// Retrieves a list of all available trophy IDs.
        /// </summary>
        /// <returns>A read-only collection of trophy IDs.</returns>
        public async Task<IReadOnlyCollection<string>> ListTrophyIdsAsync()
        {
            _logger.LogInformation("Fetching trophy IDs from the data store...");

            try
            {
                var trophyIds = await _trophyDataStore.ListTrophyIdsAsync();

                if (trophyIds.Count == 0)
                {
                    _logger.LogWarning("No trophies found in the data store.");
                    return Array.Empty<string>();
                }

                _logger.LogInformation("Successfully retrieved {Count} trophy IDs.", trophyIds.Count);
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
        /// <param name="id">The unique identifier (slug) of the trophy.</param>
        /// <returns>The trophy metadata if found; otherwise, <c>null</c>.</returns>
        public async Task<TrophyMetadata?> GetTrophyByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                _logger.LogWarning("Invalid trophy ID provided.");
                throw new ArgumentException("Trophy ID cannot be null or empty.", nameof(id));
            }

            _logger.LogInformation("Fetching trophy metadata for ID: {Id}", id);

            try
            {
                var trophy = await _trophyDataStore.GetTrophyByIdAsync(id);

                if (trophy == null)
                {
                    _logger.LogWarning("No trophy found with ID: {Id}.", id);
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
                _logger.LogWarning("Invalid trophy ID provided.");
                throw new ArgumentException("Trophy ID cannot be null or empty.", nameof(id));
            }

            _logger.LogInformation("Fetching winner image for trophy ID: {Id}", id);

            try
            {
                var winnerImageStream = await _trophyDataStore.GetWinnerImageByTrophyIdAsync(id);
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

