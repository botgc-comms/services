using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Numerics;

namespace BOTGC.API.IGScrapers
{
    public class IGClubChampionshipLeaderboardReportParser : IReportParserWithMetadata<ChampionshipLeaderboardPlayerDto, CompetitionSettingsDto>
    {
        private readonly ILogger<IGClubChampionshipLeaderboardReportParser> _logger;

        public IGClubChampionshipLeaderboardReportParser(ILogger<IGClubChampionshipLeaderboardReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<ChampionshipLeaderboardPlayerDto>> ParseReport(HtmlDocument document, CompetitionSettingsDto metaData)
        {
            var players = new List<ChampionshipLeaderboardPlayerDto>();
            var table = document.DocumentNode.SelectSingleNode("//table[contains(@class, 'global') and contains(@class, 'table')]");
            if (table == null) return players;

            var allRows = table.SelectNodes("./tr|./thead/tr|tbody/tr");
            if (allRows == null || allRows.Count < 2) return players;

            var headerRow = allRows[0];
            var headerCells = headerRow.SelectNodes("./td|./th");
            if (headerCells == null) return players;

            var map = new Dictionary<string, int>();
            int colIndex = 0;
            bool hasLatest = false;
            for (int i = 0; i < headerCells.Count; i++)
            {
                var text = headerCells[i].InnerText.Trim().ToLowerInvariant();
                int colspan = 1;
                if (headerCells[i].Attributes.Contains("colspan"))
                    int.TryParse(headerCells[i].GetAttributeValue("colspan", "1"), out colspan);

                if (text.Contains("result"))
                {
                    map["position"] = colIndex;
                    map["name"] = colIndex + 1;
                    colIndex += colspan;
                    continue;
                }
                if (text.Contains("gross leaderboard"))
                {
                    map["position"] = colIndex;
                    map["name"] = colIndex + 2;
                    colIndex += colspan;
                    continue;
                }
                if (text == "latest") hasLatest = true;
                if (text == "total" && !map.ContainsKey("par") && hasLatest)
                {
                    map["par"] = colIndex;
                }
                if (text == "total") map["total"] = colIndex;
                if (text == "status" && !map.ContainsKey("par"))
                    map["par"] = colIndex;
                if (text.Contains("gross") && !map.ContainsKey("par"))
                    map["gross"] = colIndex;
                if ((text == "after" || text == "thru") && !map.ContainsKey("thru"))
                    map["thru"] = colIndex;

                colIndex += colspan;
            }

            for (int i = 1; i < allRows.Count; i++)
            {
                var row = allRows[i];
                if (row.Name != "tr") continue;
                var columns = row.SelectNodes("./td");
                if (columns == null) continue;
                if (columns.Count < 3) continue;

                var player = new ChampionshipLeaderboardPlayerDto();

                if (map.ContainsKey("position") && columns.Count > map["position"])
                    player.Position = ParsePosition(columns[map["position"]].InnerText);

                if (map.ContainsKey("name") && columns.Count > map["name"])
                {
                    var html = columns[map["name"]].InnerHtml;
                    ParseNameAndHandicap(html, out var name, out var id, out _);
                    player.PlayerName = name;
                    player.PlayerId = id;
                }

                if (map.ContainsKey("total") && columns.Count > map["total"])
                    player.Countback = ExtractCountback(columns[map["total"]].OuterHtml.Trim());

                string parVal = null;
                if (map.ContainsKey("par") && columns.Count > map["par"])
                    parVal = columns[map["par"]].InnerText.Trim();

                if (hasLatest && string.IsNullOrWhiteSpace(parVal) && columns.Count > 1)
                    parVal = columns[columns.Count - 2].InnerText.Trim();

                player.Par = HtmlEntity.DeEntitize(parVal ?? "");

                var coursePar = metaData.CoursePar.TryGetValue("gents.white", out var value) ? value : (int?)null;

                if (map.ContainsKey("gross"))
                {
                    var grossStr = HtmlEntity.DeEntitize(columns[map["gross"]].InnerText.Trim());
                    player.Score = grossStr;
                }

                if (string.IsNullOrEmpty(player.Par) && map.ContainsKey("gross"))
                {
                    if (columns.Count > map["gross"] && coursePar != null)
                    {
                        var grossStr = HtmlEntity.DeEntitize(columns[map["gross"]].InnerText.Trim());
                        if (int.TryParse(grossStr, out int grossScore))
                        {
                            int diff = grossScore - coursePar.Value;
                            if (diff == 0)
                                player.Par = "LEVEL";
                            else if (diff > 0)
                                player.Par = $"+{diff}";
                            else
                                player.Par = diff.ToString();
                        }
                    }

                    player.Countback = ExtractCountback(columns[map["gross"]].OuterHtml.Trim());
                }
                else if (!string.IsNullOrEmpty(player.Par) && !map.ContainsKey("gross"))
                {
                    if (coursePar != null)
                    {
                        int grossScore = 0;
                        var parStr = player.Par.Trim().ToUpperInvariant();
                        if (parStr == "LEVEL" || parStr == "EVEN" || parStr == "E")
                        {
                            grossScore = coursePar.Value;
                        }
                        else if (parStr.StartsWith("+") && int.TryParse(parStr.Substring(1), out int plus))
                        {
                            grossScore = coursePar.Value + plus;
                        }
                        else if (parStr.StartsWith("-") && int.TryParse(parStr.Substring(1), out int minus))
                        {
                            grossScore = coursePar.Value - minus;
                        }
                        else if (int.TryParse(parStr, out int numeric))
                        {
                            grossScore = coursePar.Value + numeric;
                        }

                        if (grossScore > 0)
                            player.Score = grossScore.ToString();
                    }
                }

                if (map.ContainsKey("thru") && columns.Count > map["thru"])
                    player.Thru = HtmlEntity.DeEntitize(columns[map["thru"]].InnerText.Trim());

                if (string.IsNullOrWhiteSpace(player.Thru))
                {
                    player.Thru = "18";
                }

                players.Add(player);
            }

            return players;
        }

        public Task<List<ChampionshipLeaderboardPlayerDto>> ParseReport(HtmlDocument document)
        {
            return ParseReport(document, null);
        }

        private static int? ParsePosition(string text)
        {
            var match = Regex.Match(text, @"(\d+)");
            if (match.Success) return int.Parse(match.Groups[1].Value);
            return null;
        }
        private static string ExtractCountback(string html)
        {
            var match = Regex.Match(html, @"Countback\s*Results?:\s*([^""]+)", RegexOptions.IgnoreCase);
            if (match.Success)
                return match.Groups[1].Value.Trim();
            return null;
        }

        private static void ParseNameAndHandicap(string html, out string name, out int? id, out string hcap)
        {
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var a = doc.DocumentNode.SelectSingleNode(".//a[contains(@href,'playerid=')]");
            if (a != null)
            {
                name = a.InnerText.Trim();
                var idMatch = Regex.Match(a.GetAttributeValue("href", ""), @"playerid=(\d+)");
                id = idMatch.Success ? int.Parse(idMatch.Groups[1].Value) : null;
            }
            else
            {
                name = Regex.Replace(html, "<.*?>", "").Trim();
                id = null;
            }
            var hcapMatch = Regex.Match(html, @"\((\d+)\)");
            hcap = hcapMatch.Success ? hcapMatch.Groups[1].Value : null;
        }
    }
}
