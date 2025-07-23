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

    public string GetLeaderboardUrl(CompetitionSettings competitionSettings)
    {
        if (competitionSettings == null)
            throw new ArgumentNullException(nameof(competitionSettings));

        if (competitionSettings.MultiPartCompetition != null && competitionSettings.MultiPartCompetition.Any())
        {
            if (Regex.IsMatch(competitionSettings.Name, "^.*?TEST.*?DO\\sNOT\\sENTER.*?MULTI.*?$", RegexOptions.IgnoreCase))
            {
                return $"/clubchampionshipleaderboard?competitionId={competitionSettings.Id}";
            }
            else if (Regex.IsMatch(competitionSettings.Name, "^.*Championship.*?$",  RegexOptions.IgnoreCase))
            {
                return $"/clubchampionshipleaderboard?competitionId={competitionSettings.Id}";
            }
        }

        return $"/leaderboard?competitionId={competitionSettings.Id}";
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

        // Step 1: Gather all child competition IDs from MultiPartCompetition dictionaries.
        var childIds = parsedData!
            .SelectMany(c => c.MultiPartCompetition?.Values ?? Enumerable.Empty<int>())
            .ToHashSet();

        // Step 2: Only keep competitions whose ID does NOT appear as a child.
        var filteredCompetitions = parsedData!
            .Where(c => !childIds.Contains(c.Id ?? 0))
            .ToList();

        // Step 3: Map to your view model.
        var retVal = filteredCompetitions
            .Select(c => new CompetitionDetailsViewModel
            {
                Id = c.Id ?? 0,
                Name = c.Name,
                Date = c.Date,
                Format = c.Format,
                ResultsDisplay = c.ResultsDisplay,
                LeaderboardUrl = GetLeaderboardUrl(c)
            })
            .ToList();

        return retVal;
    }

    public async Task<LeaderboardViewModel?> GetLeaderboardPlayersAsync(int competitionId)
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

    public async Task<ClubChampionshipLeaderboardViewModel?> GetClubChampionshipLeaderboardPlayersAsync(int competitionId)
    {
        try
        {
            var serviceUrl = $"{_settings.API.Url}/api/competitions/{competitionId}/clubChampionshipsLeaderboard";

            var request = new HttpRequestMessage(HttpMethod.Get, serviceUrl);
            request.Headers.Add("X-API-KEY", _settings.API.XApiKey);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var parsedData = JsonSerializer.Deserialize<ClubChampionshipsLeaderboardResultsRoot>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            // No custom sorting/grouping—use positions as given
            var players = parsedData.Total
                .Select(p => new PlayerMultiRoundViewModel
                {
                    Name = p.PlayerName,
                    R1 = p.R1 ?? "",
                    R2 = p.R2 ?? "",
                    Par = p.Par ?? "",
                    Thru = ParseThru(p.Thru),
                    Position = p.Position ?? 0
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

            return new ClubChampionshipLeaderboardViewModel
            {
                Players = players,
                CompetitionDetails = competitionDetails
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch or parse leaderboard data");
            return null;
        }

        int ParseThru(string thru)
        {
            if (string.IsNullOrWhiteSpace(thru)) return 0;
            if (int.TryParse(thru, out var n)) return n;
            return 0;
        }
    }

}
