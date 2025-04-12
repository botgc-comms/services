using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Leaderboards.Models;

namespace Leaderboards.Controllers
{
    [Route("[controller]")]
    public class JuniorEclecticController : Controller
    {
        private readonly string _filePath = "C:\\_src\\BOTGC\\git\\Services\\Cache\\Junior_Eclectic_Results_20240101_20241231.json";

        public IActionResult Index()
        {
            if (!System.IO.File.Exists(_filePath))
            {
                ViewBag.ErrorMessage = "Data file not found.";
                return View("Error"); // Return an error view if file is missing
            }

            try
            {
                // Read JSON file
                var jsonData = System.IO.File.ReadAllText(_filePath);
                var parsedData = JsonSerializer.Deserialize<EclecticResultsRoot>(jsonData, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (parsedData?.Value?.Scores == null)
                {
                    ViewBag.ErrorMessage = "Invalid data format.";
                    return View("Error");
                }

                // Convert JSON data to ViewModel
                var eclecticPlayers = parsedData.Value.Scores.Select(score => new EclecticPlayerViewModel
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
                                ExclusionReasons = score.ExcludedRounds?.Select(e => e.ExclusionReason).ToList() ?? new List<string>()
                            }).ToList()
                    }
                }).OrderByDescending(p => p.TotalScore).ToList();

                return View(eclecticPlayers); // Return the leaderboard view with model
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"Error processing file: {ex.Message}";
                return View("Error");
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

    // Root JSON Structure
    public class EclecticResultsRoot
    {
        public ValueWrapper Value { get; set; }
    }

    public class ValueWrapper
    {
        public List<ScoreEntry> Scores { get; set; }
    }

    public class ScoreEntry
    {
        public ScorecardModel Scorecard { get; set; }
        public List<ExcludedRound> ExcludedRounds { get; set; }
    }

    public class ScorecardModel
    {
        public string PlayerName { get; set; }
        public int TotalStablefordScore { get; set; }
        public List<HoleModel> Holes { get; set; }
    }

    public class HoleModel
    {
        public int HoleNumber { get; set; }
        public DateTime RoundDate { get; set; }
        public int StablefordScore { get; set; }
    }

    public class ExcludedRound
    {
        public string MemberId { get; set; }
        public string RoundId { get; set; }
        public string Type { get; set; }
        public DateTime DatePlayed { get; set; }
        public string ExclusionReason { get; set; }
    }
}
