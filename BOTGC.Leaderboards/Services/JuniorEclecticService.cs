using BOTGC.Leaderboards;
using BOTGC.Leaderboards.Interfaces;
using BOTGC.Leaderboards.Models.EclecticScorecard;

using Microsoft.Extensions.Options;

using System.Text.Json;

namespace BOTGC.Leaderboards.Services;

public class JuniorEclecticService : IJuniorEclecticService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<JuniorEclecticService> _logger;
    private readonly AppSettings _settings;

    public JuniorEclecticService(HttpClient httpClient, ILogger<JuniorEclecticService> logger, IOptions<AppSettings> settings)
    {
        _httpClient = httpClient;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<List<EclecticPlayerViewModel>> GetPlayersAsync(int? year = null)
    {
        try
        {
            var targetYear = year ?? DateTime.UtcNow.Year;
            var fromDate = new DateTime(targetYear, 1, 1).ToString("yyyy-MM-dd");
            var toDate = new DateTime(targetYear, 12, 31).ToString("yyyy-MM-dd");

            var serviceUrl = $"{_settings.API.Url}/api/competitions/juniorEclectic/results?fromDate={fromDate}&toDate={toDate}";

            var request = new HttpRequestMessage(HttpMethod.Get, serviceUrl);
            request.Headers.Add("x-api-key", _settings.API.XApiKey);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();

            var parsedData = JsonSerializer.Deserialize<EclecticResultsRoot>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsedData?.Value?.Scores == null)
                return new List<EclecticPlayerViewModel>();

            return parsedData.Value.Scores.Select(score => new EclecticPlayerViewModel
            {
                PlayerName = score.Scorecard.PlayerName,
                BestFrontNine = GetBestNine(score.Scorecard.Holes, 1, 9),
                BestBackNine = GetBestNine(score.Scorecard.Holes, 10, 18),
                FrontNineCards = GetCardCount(score.Scorecard.Holes, 1, 9),
                BackNineCards = GetCardCount(score.Scorecard.Holes, 10, 18),
                TotalScore = score.Scorecard.TotalStablefordScore,
                TotalScoreCountBack = score.Scorecard.Holes.Sum(h => h.StablefordScore),
                Scores = new ScoreBreakdownViewModel
                {
                    ScoreCards = score.Scorecard.Holes.GroupBy(h => h.RoundDate)
                        .Select(g => new ScoreCardViewModel
                        {
                            PlayedOn = g.Key,
                            IsIncluded = true,
                            Holes = g.Select(h => new HoleViewModel
                            {
                                HoleNumber = h.HoleNumber,
                                HoleScore = h.StablefordScore,
                                IsSelected = true
                            }).ToList(),
                            ExclusionReasons = score.ExcludedRounds?.Select(e => e.ExclusionReason).ToList()
                                ?? new List<string>()
                        }).ToList()
                }
            }).OrderByDescending(p => p.TotalScore).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch or parse Junior Eclectic data");
            return new List<EclecticPlayerViewModel>();
        }
    }

    private int GetBestNine(IEnumerable<HoleModel> holes, int startHole, int endHole)
    {
        return holes.Where(h => h.HoleNumber >= startHole && h.HoleNumber <= endHole)
                    .Sum(h => h.StablefordScore);
    }

    private int GetCardCount(IEnumerable<HoleModel> holes, int startHole, int endHole)
    {
        return holes.Where(h => h.HoleNumber >= startHole && h.HoleNumber <= endHole)
                    .GroupBy(h => h.RoundDate)
                    .Count();
    }
}
