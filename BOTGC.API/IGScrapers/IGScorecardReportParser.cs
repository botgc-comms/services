using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using HtmlAgilityPack;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BOTGC.API.IGScrapers
{
    /// <summary>
    /// Parses the IG report HTML document to extract golf scorecard data.
    /// </summary>
    public class IGScorecardReportParser : IReportParser<ScorecardDto>
    {
        private readonly ILogger<IGScorecardReportParser> _logger;

        public IGScorecardReportParser(ILogger<IGScorecardReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<ScorecardDto>?> ParseReport(HtmlDocument document)
        {
            var scorecards = new List<ScorecardDto>();

            var scorecardTable = document.DocumentNode.SelectSingleNode("//table[@class='table table-striped']");
            if (scorecardTable == null)
            {
                _logger.LogWarning("Unable to locate scorecard table in the report.");
                return null;
            }

            var headers = scorecardTable.SelectSingleNode(".//thead/tr/td")?.InnerText.Trim();
            if (string.IsNullOrEmpty(headers))
            {
                _logger.LogError("Failed to extract player details from the scorecard.");
                return null;
            }

            try
            {
                var roundId = 0;
                var roundIdNodes = document.DocumentNode.SelectNodes("//a");
                var roundIdNode = roundIdNodes.FirstOrDefault(a => Regex.IsMatch(a.OuterHtml, "admincontact[.]php[^\"]*roundid(?:=|%3D)\\d+"));
                if (roundIdNode != null)
                {
                    roundId = int.Parse(Regex.Match(roundIdNode.OuterHtml, "admincontact[.]php[^\"]*roundid(?:=|%3D)(\\d+)", RegexOptions.IgnoreCase).Groups[1].Value);
                }
                else
                {
                    _logger.LogError("Unable to locate round id.");
                    return null;
                }

                var shotsReceived = ExtractShotsReceived(headers);
                if (shotsReceived == null)
                {
                    _logger.LogError("Failed to determine the number of shots received.");
                    return null;
                }

                // Extract top-level details from header row
                var scorecard = new ScorecardDto
                {
                    RoundId = roundId,
                    PlayerName = ExtractPlayerName(headers),
                    ShotsReceived = (int)shotsReceived,
                    HandicapAllowance = ExtractHandicapAllowance(headers),
                    CompetitionName = ExtractCompetitionName(headers),
                    DatePlayed = ExtractCardDate(headers),
                    TeeColour = ExtractTeeColour(scorecardTable)
                };

                _logger.LogInformation("Parsed scorecard for {Player}", scorecard.PlayerName);

                // Extract hole-by-hole details
                var holeDataRows = scorecardTable.SelectNodes(".//tr[@class='holedata']");
                if (holeDataRows == null || holeDataRows.Count < 4)
                {
                    _logger.LogWarning("Insufficient hole data found.");
                    return null;
                }

                var yardageRow = holeDataRows[0].SelectNodes(".//td")?.Skip(1).ToArray();
                var strokeIndexRow = holeDataRows[1].SelectNodes(".//td")?.Skip(1).ToArray();
                var parRow = holeDataRows[2].SelectNodes(".//td")?.Skip(1).ToArray();
                var scoreRow = holeDataRows[3].SelectNodes(".//td")?.Skip(1).ToArray();
                var stablefordRow = holeDataRows.Count >= 5 ? holeDataRows[4].SelectNodes(".//td")?.Skip(1).ToArray() : null;

                if (yardageRow == null || strokeIndexRow == null || parRow == null || scoreRow == null)
                {
                    _logger.LogError("Hole data could not be parsed.");
                    return null;
                }

                var holes = yardageRow.Count() > 11 ? 18 : 9;

                var GetHole = (int i, int h) =>
                {
                    var hole = new ScorecardHoleDto
                    {
                        HoleNumber = h,
                        Yardage = int.TryParse(yardageRow[i]?.InnerText, out var y) ? y : 0,
                        StrokeIndex = int.TryParse(strokeIndexRow[i]?.InnerText, out var si) ? si : 0,
                        Par = int.TryParse(parRow[i]?.InnerText, out var p) ? p : 0,
                        Gross = scoreRow[i]?.InnerText?.Trim() ?? "NS",
                    };

                    var baseShots = scorecard.ShotsReceived / holes;
                    var extraShot = scorecard.ShotsReceived % holes >= hole.StrokeIndex ? 1 : 0;
                    hole.ShotsReceived = baseShots + extraShot;

                    hole.Net = "NS";

                    int strokes;
                    if (int.TryParse(hole.Gross, out strokes))
                    {
                        hole.Net = (strokes - hole.ShotsReceived).ToString();
                    }

                    if (stablefordRow != null)
                    {
                        hole.StablefordScore = int.TryParse(stablefordRow[i]?.InnerText, out var sf) ? sf : 0;
                    }
                    else
                    {
                        var net = int.TryParse(hole.Net, out int s) ? s : 10;
                        hole.StablefordScore = Math.Max(0, 2 + (hole.Par - net));
                    }

                    return hole;
                };

                for (int i = 0; i < 9; i++)
                {
                    scorecard.Holes.Add(GetHole(i, i + 1));
                }

                if (holes == 18)
                {
                    for (int i = 10; i < 19; i++)
                    {
                        scorecard.Holes.Add(GetHole(i, i));
                    }
                }

                // Extract total scores
                scorecard.TotalStrokes = int.TryParse(holeDataRows[3].SelectSingleNode(".//td[@class='lastcol']")?.InnerText, out var totalStrokes) ? totalStrokes : 0;

                if (stablefordRow != null)
                {
                    scorecard.TotalStablefordScore = int.TryParse(holeDataRows[4].SelectSingleNode(".//td[@class='lastcol']")?.InnerText, out var totalStableford) ? totalStableford : 0;
                }
                else
                {
                    scorecard.TotalStablefordScore = scorecard.Holes.Sum(h => h.StablefordScore);
                }

                scorecards.Add(scorecard);
                _logger.LogInformation("Successfully parsed scorecard for {Player}", scorecard.PlayerName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing scorecard data.");
            }

            return scorecards;
        }

        private static string ExtractPlayerName(string headerText)
        {
            var text = Regex.Replace(headerText, @"\s*((?:Mon|Tues|Wed|Thur|Fri|Sat|Sun)\w*\s+\d+(?:th|nd|st|th)?\s(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sept|Oct|Nov|Dec)\w*\s+\d+)", "");
            text = Regex.Replace(text, @"\s*\([^)]*\)\s*", " ");
            text = Regex.Replace(text, @"\s*\d+\s*$", " ");
            text = Regex.Match(text, @"^([^:\d]+)").Groups[1].Value;
            text = Regex.Replace(text, "\\s{2,}", " ");

            return text.Trim();
        }

        private static int? ExtractShotsReceived(string headerText)
        {
            var text = Regex.Replace(headerText, @"\s*((?:Mon|Tues|Wed|Thur|Fri|Sat|Sun)\w*\s+\d+(?:th|nd|st|th)?\s(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sept|Oct|Nov|Dec)\w*\s+\d+)", "");
            text = Regex.Match(text, @"^([^:]+)").Groups[1].Value;
            text = Regex.Replace(text, @"\s*\([^)]*allowance[^)]*\)\s*", " ");
            text = Regex.Replace(text, @"[()]", " ");
            text = Regex.Replace(text, "\\s{2,}", " ");

            var match = Regex.Match(text, @"(\d+)\s*$");
            if (match.Success)
            {
                var shotsReceived = int.Parse(match.Groups[1].Value);
                if (shotsReceived <= 54)
                {
                    return shotsReceived;
                }
            }

            return null;
        }

        private static string ExtractHandicapAllowance(string headerText)
        {
            var match = Regex.Match(headerText, @"[(](\d+\%)[^)]*allowance");
            return match.Success ? match.Groups[1].Value : "N/A";
        }

        private static string ExtractCompetitionName(string headerText)
        {
            var text = Regex.Replace(headerText, @"\s*((?:Mon|Tues|Wed|Thur|Fri|Sat|Sun)\w*\s+\d+(?:th|nd|st|th)?\s(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sept|Oct|Nov|Dec)\w*\s+\d+)", "");
            var match = Regex.Match(text, @":\s*(.*?)\s*$");
            return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
        }

        private static DateTime ExtractCardDate(string headerText)
        {
            var match = Regex.Match(headerText, @"((?:Mon|Tues|Wed|Thur|Fri|Sat|Sun)\w*\s+\d+(?:th|nd|st|th)?\s(?:Jan|Feb|Mar|Apr|May|Jun|Jul|Aug|Sept|Oct|Nov|Dec)\w*\s+\d+)");
            var dateText = Regex.Replace(match.Groups[1].Value, @"(\d+)(?:st|nd|rd|th)", "$1");
            dateText = Regex.Replace(dateText, "\\s{2,}", " ");
            return match.Success && DateTime.TryParseExact(dateText, "dddd d MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : DateTime.MinValue;
        }

        private static string ExtractTeeColour(HtmlNode table)
        {
            var match = Regex.Match(table.InnerHtml, @"(White|Yellow|Red|Green|Black)\sTees?");
            return match.Success ? match.Groups[1].Value : "Unknown";
        }
    }
}
