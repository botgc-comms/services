using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Services.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Services.Services;

/// <summary>
/// Handles reading trophy metadata from a GitHub repository.
/// </summary>
public class TrophyFilesGitHub
{
    private readonly AppSettings _settings;
    private readonly ILogger<TrophyFilesGitHub> _logger;
    private readonly HttpClient _httpClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="TrophyFilesDiskStorage"/> class.
    /// </summary>
    /// <param name="settings">Application settings.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="httpClient">HTTP client for GitHub API requests.</param>
    public TrophyFilesGitHub(IOptions<AppSettings> settings, ILogger<TrophyFilesGitHub> logger, HttpClient httpClient)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
    }

    /// <summary>
    /// Retrieves a list of all trophies by reading metadata from a GitHub repository.
    /// </summary>
    /// <returns>A collection of <see cref="TrophyMetadata"/>.</returns>
    public async Task<IReadOnlyCollection<TrophyMetadata>> ListTrophiesAsync()
    {
        if (string.IsNullOrWhiteSpace(_settings.GitHub.RepoUrl))
        {
            _logger.LogError("GitHub repository URL is not configured.");
            return Array.Empty<TrophyMetadata>();
        }

        var trophies = new ConcurrentBag<TrophyMetadata>();

        try
        {
            var trophyDirs = await GetTrophyDirectoriesAsync();
            if (!trophyDirs.Any())
            {
                _logger.LogWarning("No trophies found in the GitHub repository.");
                return Array.Empty<TrophyMetadata>();
            }

            var tasks = trophyDirs.Select(async dir =>
            {
                var metadata = await GetTrophyMetadataAsync(dir);
                if (metadata != null)
                {
                    trophies.Add(metadata);
                }
            });

            await Task.WhenAll(tasks);
            _logger.LogInformation("Successfully loaded {Count} trophies from GitHub.", trophies.Count);
            return trophies.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load trophies from GitHub repository.");
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
        }

        return trophy;
    }

    /// <summary>
    /// Fetches the list of trophy directories from the GitHub repository.
    /// </summary>
    private async Task<List<string>> GetTrophyDirectoriesAsync()
    {
        var apiUrl = $"{_settings.GitHub.ApiUrl}/contents/{_settings.GitHub.TrophyDirectory}";
        _logger.LogInformation("Fetching trophy directories from GitHub: {ApiUrl}", apiUrl);

        try
        {
            var response = await _httpClient.GetAsync(apiUrl);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            var directories = JsonSerializer.Deserialize<List<GitHubContent>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return directories?
                .Where(d => d.Type == "dir")
                .Select(d => d.Name)
                .ToList() ?? new List<string>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching trophy directories from GitHub.");
            return new List<string>();
        }
    }

    /// <summary>
    /// Retrieves and parses trophy metadata from GitHub.
    /// </summary>
    private async Task<TrophyMetadata?> GetTrophyMetadataAsync(string directoryName)
    {
        var metadataUrl = $"{_settings.GitHub.RawUrl}/{_settings.GitHub.TrophyDirectory}/{directoryName}/metadata.json";
        _logger.LogInformation("Fetching trophy metadata from GitHub: {MetadataUrl}", metadataUrl);

        try
        {
            var response = await _httpClient.GetAsync(metadataUrl);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Metadata file not found for trophy: {DirectoryName}", directoryName);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync();
            var metadata = JsonSerializer.Deserialize<TrophyMetadata>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (metadata == null)
            {
                _logger.LogWarning("Invalid metadata format for trophy: {DirectoryName}", directoryName);
                return null;
            }

            metadata.ImageUrl = $"{_settings.GitHub.RawUrl}/{_settings.GitHub.TrophyDirectory}/{directoryName}/{metadata.ImageUrl}";
            metadata.WinnerImage = $"{_settings.GitHub.RawUrl}/{_settings.GitHub.TrophyDirectory}/{directoryName}/{metadata.WinnerImage}";

            return metadata;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching metadata for trophy: {DirectoryName}", directoryName);
            return null;
        }
    }
}

/// <summary>
/// Represents a GitHub API directory or file response.
/// </summary>
public class GitHubContent
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "file" or "dir"
}


