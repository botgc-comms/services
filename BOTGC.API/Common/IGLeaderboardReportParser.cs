using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace BOTGC.API.Common
{
    public class IGLeaderboardReportParser : IReportParserWithMetadata<LeaderBoardDto, CompetitionSettingsDto>
    {
        private readonly ILogger<IGLeaderboardReportParser> _logger;

        public IGLeaderboardReportParser(ILogger<IGLeaderboardReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Parses an HTML document and extracts leaderboard data.
        /// </summary>
        /// <param name="document">The HTML document containing the leaderboard table.</param>
        /// <returns>A list of parsed <see cref="LeaderBoardDto"/> objects.</returns>
        public async Task<List<LeaderBoardDto>> ParseReport(HtmlDocument document, CompetitionSettingsDto metaData)
        {
            var format = metaData != null ? metaData.Format.ToLower() : "stablefordS";

            var leaderboard = new LeaderBoardDto
            {
                Players = new List<LeaderboardPlayerDto>()
            };

            // Find the table
            var table = document.DocumentNode.SelectSingleNode("//table[contains(@class, 'table')]");
            if (table == null)
            {
                _logger.LogWarning("Leaderboard table not found.");
                return new List<LeaderBoardDto> { leaderboard };
            }

            // Find header row (thead or first tr)
            var headerRow = table.SelectSingleNode(".//thead/tr") ?? table.SelectSingleNode(".//tr");
            if (headerRow == null)
            {
                _logger.LogWarning("Leaderboard header row not found.");
                return new List<LeaderBoardDto> { leaderboard };
            }

            // Find all data rows (skip header)
            var dataRows = table.SelectNodes(".//tr[td]")?.Skip(1).ToList();
            if (dataRows == null || dataRows.Count == 0)
            {
                _logger.LogWarning("No leaderboard data rows found.");
                return new List<LeaderBoardDto> { leaderboard };
            }

            // Use the first data row to help map columns
            var columnMap = BuildColumnMap(headerRow, dataRows[0], metaData);

            // Find all data rows (skip header)
            if (dataRows == null || dataRows.Count == 0)
            {
                _logger.LogWarning("No leaderboard data rows found.");
                return new List<LeaderBoardDto> { leaderboard };
            }

            foreach (var row in dataRows)
            {
                var columns = row.SelectNodes("./td");
                if (columns == null) continue;

                try
                {
                    var player = new LeaderboardPlayerDto();

                    for (int i = 0; i < columns.Count; i++)
                    {
                        var colType = columnMap.TryGetValue(i, out var type) ? type : null;
                        var html = columns[i].InnerHtml.Trim();

                        switch (colType)
                        {
                            case "position":
                                player.Position = ExtractPosition(html);
                                break;
                            case "name":
                                if (format.Contains("team"))
                                {
                                    player.PlayerName = ExtractTeamNames(html);
                                    player.PlayingHandicap = ExtractTeamHandicaps(html);
                                }
                                else
                                {
                                    player.PlayerName = ExtractPlayerName(html);
                                    player.PlayerId = ExtractPlayerId(html);
                                    player.PlayingHandicap = ExtractPlayerHandicap(html);
                                }
                                break;
                            case "score":
                            case "net":
                                if (format.Contains("medal"))
                                {
                                    player.NetScore = ExtractNetScore(html);
                                }
                                else
                                {
                                    player.StablefordScore = ExtractStablefordScore(html);
                                }
                                player.Countback = ExtractCountback(html);
                                break;

                            case "thru":
                                player.Thru = ExtractThru(html);
                                break;
                        }
                    }

                    leaderboard.Players.Add(player);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing player row: {RowHtml}", row.InnerHtml);
                }
            }

            _logger.LogInformation("Successfully parsed leaderboard including {players} players.", leaderboard.Players.Count);
            return new List<LeaderBoardDto> { leaderboard };
        }

        /// <summary>
        /// Analyze the header row and map column indices to data types.
        /// </summary>
        private static Dictionary<int, string> BuildColumnMap(
            HtmlNode headerRow,
            HtmlNode firstDataRow,
            CompetitionSettingsDto settings)
        {
            var map = new Dictionary<int, string>();
            var headers = headerRow.SelectNodes("./td|./th");
            var dataCells = firstDataRow.SelectNodes("./td|./th");
            if (headers == null || dataCells == null) return map;

            // Extract context from settings
            var format = (settings?.Format ?? "").ToLowerInvariant();
            var resultsDisplay = (settings?.ResultsDisplay ?? "").ToLowerInvariant();
            var tags = settings?.Tags ?? new string[0];

            // Expand header cells to match data columns, accounting for colspan
            var expandedHeaders = new List<string>();
            foreach (var header in headers)
            {
                int colspan = 1;
                var colspanAttr = header.GetAttributeValue("colspan", "1");
                int.TryParse(colspanAttr, out colspan);
                for (int i = 0; i < colspan; i++)
                    expandedHeaders.Add(header.InnerText.Trim().ToLowerInvariant());
            }

            for (int i = 0; i < dataCells.Count; i++)
            {
                string text = expandedHeaders.Count > i ? expandedHeaders[i] : "";

                // Use both header and settings for mapping
                if (i == 0 || text.Contains("position") || text.Contains("pos") || Regex.IsMatch(text, @"^\d+(st|nd|rd|th)?$"))
                    map[i] = "position";
                else if (i == 1 || text.Contains("player") || text.Contains("name") || text.Contains("team"))
                    map[i] = "name";
                else if (text.Contains("status"))
                    map[i] = "status";
                else if (text.Contains("thru"))
                    map[i] = "thru";
                else if (text.Contains("nett") || text.Contains("net"))
                {
                    if (format.Contains("medal") || format.Contains("team") || format.Contains("pairs"))
                        map[i] = "net";
                    else
                        map[i] = "score";
                }
                else if (text.Contains("gross"))
                    map[i] = "gross";
                else if (text.Contains("points"))
                {
                    if (format.Contains("stableford"))
                        map[i] = "score";
                    else
                        map[i] = "points";
                }
                else if (text.Contains("total"))
                {
                    if (format.Contains("medal") || format.Contains("team") || format.Contains("pairs"))
                        map[i] = "net";
                    else if (format.Contains("stableford"))
                        map[i] = "score";
                    else
                        map[i] = "score";
                }
                else if (text.Contains("result"))
                {
                    // "Results" is often ambiguous, but in your HTML it's usually position/name/score
                    if (i == 0)
                        map[i] = "position";
                    else if (i == 1)
                        map[i] = "name";
                    else
                        map[i] = (format.Contains("medal") || format.Contains("team") || format.Contains("pairs")) ? "net" : "score";
                }
                else
                    map[i] = "unknown";
            }

            return map;
        }

        private static string ExtractCountback(string html)
        {
            var match = Regex.Match(html, @"Countback\s*Results?:\s*([^""]+)", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();
            return null;
        }

        private static string ExtractThru(string html)
        {
            var match = Regex.Match(html, @"(\d+)", RegexOptions.Multiline);
            if (match.Success)
                return match.Groups[1].Value;
            return null;
        }

        private static string ExtractPlayerName(string html)
        {
            var match = Regex.Match(html, "<[^>]*playerid=[^>]*?>([^<]+)<");
            if (match.Success)
                return match.Groups[1].Value.Trim();
            // fallback: plain text
            return HtmlEntity.DeEntitize(Regex.Replace(html, "<.*?>", "")).Trim();
        }

        private static string ExtractTeamNames(string html)
        {
            // Remove HTML tags, return the names part (e.g. "John Smith (12) & Jane Doe (14)")
            return HtmlEntity.DeEntitize(Regex.Replace(html, "<.*?>", "")).Trim();
        }

        private static string ExtractPlayerHandicap(string html)
        {
            var match = Regex.Match(html, @"\((\d+)\)");
            if (match.Success)
                return match.Groups[1].Value;
            return null;
        }

        private static string ExtractTeamHandicaps(string html)
        {
            // Extract all handicaps in the string, join with comma
            var matches = Regex.Matches(html, @"\((\d+)\)");
            if (matches.Count > 0)
                return string.Join(",", matches.Select(m => m.Groups[1].Value));
            return null;
        }

        private static string ExtractStablefordScore(string html)
        {
            var match = Regex.Match(html, @"<a[^>]*>([^<]+)<");
            if (match.Success)
                return match.Groups[1].Value.Trim();
            match = Regex.Match(html, @"(\d+)");
            return match.Success ? match.Groups[1].Value : null;
        }

        private static string? ExtractNetScore(string html)
        {
            // Try to extract from <a> tag first
            var match = Regex.Match(html, @"<a[^>]*>([^<]+)<");
            if (match.Success)
                return match.Groups[1].Value.Trim();

            // Fallback: plain text (could be "LEVEL", "NR", "DQ", or a number)
            var text = HtmlEntity.DeEntitize(Regex.Replace(html, "<.*?>", "")).Trim();
            return !string.IsNullOrEmpty(text) ? text : null;
        }


        private static int? ExtractPosition(string html)
        {
            var match = Regex.Match(html, @"(\d+)(?:nd|st|th|rd)");
            if (match.Success)
                return int.Parse(match.Groups[1].Value);
            // fallback: just digits
            match = Regex.Match(html, @"(\d+)");
            if (match.Success)
                return int.Parse(match.Groups[1].Value);
            return null;
        }

        private static int? ExtractPlayerId(string html)
        {
            var match = Regex.Match(html, @"<[^>]*playerid=(\d+)");
            if (match.Success)
                return int.Parse(match.Groups[1].Value);
            return null;
        }

        public Task<List<LeaderBoardDto>> ParseReport(HtmlDocument document)
        {
            return this.ParseReport(document, null);
        }
    }
}
