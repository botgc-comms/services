using BOTGC.Leaderboards;
using BOTGC.Leaderboards.Interfaces;
using BOTGC.Leaderboards.Models;
using BOTGC.Leaderboards.Models.EclecticScorecard;

using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace BOTGC.Leaderboards.Services;

public class LeaderboardService : ILeaderboardService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<LeaderboardService> _logger;
    private readonly AppSettings _settings;

    public LeaderboardService(HttpClient httpClient, ILogger<LeaderboardService> logger, IOptions<AppSettings> settings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<List<CompetitionDetailsViewModel>> GetCompetitionsAsync()
    {
        var serviceUrl = $"{_settings.API.Url}/api/competitions";

        var request = new HttpRequestMessage(HttpMethod.Get, serviceUrl);
        request.Headers.Add("X-API-KEY", _settings.API.XApiKey);

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();

        var parsedData = JsonSerializer.Deserialize<List<CompetitionSettings>>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        return parsedData!.Select(c => new CompetitionDetailsViewModel
        {
            Id = c.Id ?? 0,
            Name = c.Name,
            Date = c.Date,
            Format = c.Format,
            ResultsDisplay = c.ResultsDisplay
        }).ToList();
    }

    public async Task<LeaderboardViewModel?> GetPlayersAsync(int competitionId)
    {
        try
        {
            var serviceUrl = $"{_settings.API.Url}/api/competitions/{competitionId}/leaderboard";

            var request = new HttpRequestMessage(HttpMethod.Get, serviceUrl);
            request.Headers.Add("X-API-KEY", _settings.API.XApiKey);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var parsedData = JsonSerializer.Deserialize<LeaderboardResultsRoot>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            var playerViewModels = parsedData.Players
                .Where(p => string.IsNullOrEmpty(p.Thru) || p.Thru == "18")
                .Select((p, idx) => new PlayerViewModel
                {
                    Position = idx + 1,
                    Names = p.PlayerName,
                    PlayingHandicap = p.PlayingHandicap,
                    Score = p.NetScore ?? p.StablefordScore,
                    Countback = p.Countback
                })
                .ToList();

            var competitionDetails = new CompetitionDetailsViewModel
            {
                Id = parsedData.CompetitionDetails.Id ?? competitionId,
                Name = Regex.Replace(parsedData.CompetitionDetails.Name, "[(][^)]*[)]", ""), 
                Date = parsedData.CompetitionDetails.Date,
                Format = parsedData.CompetitionDetails.Format,
                ResultsDisplay = parsedData.CompetitionDetails.ResultsDisplay   
            };

            return new LeaderboardViewModel
            {
                Players = playerViewModels, 
                CompetitionDetails = competitionDetails
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch or parse leaderboard data");
            return null;
        }
    }
}
