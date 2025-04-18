using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace BOTGC.API.Common
{
    public class IGLeaderboardReportParser: IReportParser<LeaderBoardDto>
    {
        private readonly ILogger<IGLeaderboardReportParser> _logger;

        public IGLeaderboardReportParser(ILogger<IGLeaderboardReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Parses an HTML document and extracts round summaries.
        /// </summary>
        /// <param name="document">The HTML document containing the round table.</param>
        /// <returns>A list of parsed <see cref="RoundDto"/> objects.</returns>
        public List<LeaderBoardDto> ParseReport(HtmlDocument document)
        {
            var leaderboard = new LeaderBoardDto()
            {
                Players = new List<LeaderboardPlayerDto>()
            };

            // Locate the rows in the table
            var rows = document.DocumentNode.SelectNodes("//table/tr");
            if (rows == null || rows.Count == 0)
            {
                _logger.LogWarning("No results fround.");
                return new List<LeaderBoardDto>([leaderboard]);
            }

            foreach (var row in rows)
            {
                var columns = row.SelectNodes(".//td")?.Select(td => td.InnerHtml.Trim()).ToArray();
                if (columns == null || columns.Length < 4) continue;

                try
                {
                    var player = new LeaderboardPlayerDto
                    {
                        Position = ExtractPosition(columns[0]),
                        PlayerName = ExtractPlayerName(columns[1]), 
                        PlayerId = ExtractPlayerId(columns[1]), 
                        PlayingHandicap = ExtractPlayerHandicap(columns[1]),
                        StablefordScore = ExtractStablefordScore(columns[2]),
                        Countback = ExtractCountback(columns[2]), 
                        Thru = ExtractThru(columns[3])
                    };

                    leaderboard.Players.Add(player);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing player {RowData}", string.Join(", ", columns));
                }
            }

            _logger.LogInformation("Successfully parsed leaderboard including {players} players.", leaderboard.Players.Count);
            return new List<LeaderBoardDto>([leaderboard]);
        }

        /// <summary>
        ///  Return the countback score
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private static string ExtractCountback(string html)
        {
            var match = Regex.Match(html, "<[^>]*Countback\\sresults:([^\"]+)", RegexOptions.Multiline);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return null;
        }

        /// <summary>
        ///  Return the thur holes
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private static string ExtractThru(string html)
        {
            var match = Regex.Match(html, "(\\d+)", RegexOptions.Multiline);
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return null;
        }

        /// <summary>
        ///  Return the name of the player
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private static string ExtractPlayerName(string html)
        {
            var match = Regex.Match(html, "<[^>]*playerid=[^>]*?>([^<]+)<");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return null;
        }

        /// <summary>
        ///  Return the stableford score
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private static string ExtractStablefordScore(string html)
        {
            var match = Regex.Match(html, "<a[^>]>([^<]+)<");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return null;
        }

        /// <summary>
        ///  Return the players handicap
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private static string ExtractPlayerHandicap(string html)
        {
            var match = Regex.Match(html, @"\([\d+]\)");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            return null;
        }

        /// <summary>
        ///  Return the name of the player
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private static int? ExtractPosition(string html)
        {
            var match = Regex.Match(html, @"(\d+)(?:nd|st|th|rd)");
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value);
            }
            return null;
        }

        /// <summary>
        ///  Return the id of the player
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private static int? ExtractPlayerId(string html)
        {
            var match = Regex.Match(html, @"<[^>]*playerid=(\d+)");
            if (match.Success)
            {
                return int.Parse(match.Groups[1].Value);
            }
            return null;
        }
    }
}
