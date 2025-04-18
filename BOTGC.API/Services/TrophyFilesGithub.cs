using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;

namespace BOTGC.API.Services;

public class TrophyFilesGitHub : ITrophyFiles
{
    private readonly AppSettings _settings;
    private readonly ILogger<TrophyFilesGitHub> _logger;
    private readonly HttpClient _httpClient;

    public TrophyFilesGitHub(IOptions<AppSettings> settings, ILogger<TrophyFilesGitHub> logger, HttpClient httpClient)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));

        // Ensure GitHub API requests are authenticated.
        if (!string.IsNullOrEmpty(_settings.GitHub.Token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("token", _settings.GitHub.Token);
        }

        // GitHub requires a User-Agent header.
        if (!_httpClient.DefaultRequestHeaders.UserAgent.Any())
        {
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("BOTGC.API");
        }
    }

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

        try
        {
            _logger.LogInformation("Downloading winner image for trophy ID: {Id}", id);
            var response = await _httpClient.GetAsync(trophy.WinnerImage);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download winner image for trophy ID: {Id}", id);
            return null;
        }
    }
}

public class GitHubContent
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // "file" or "dir"
}
