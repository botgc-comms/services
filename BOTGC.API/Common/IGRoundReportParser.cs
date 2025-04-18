using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using HtmlAgilityPack;
using System.Globalization;
using System.Text.RegularExpressions;

namespace BOTGC.API.Common
{
    public class IGRoundReportParser: IReportParser<RoundDto>
    {
        private readonly ILogger<IGRoundReportParser> _logger;

        public IGRoundReportParser(ILogger<IGRoundReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Parses an HTML document and extracts round summaries.
        /// </summary>
        /// <param name="document">The HTML document containing the round table.</param>
        /// <returns>A list of parsed <see cref="RoundDto"/> objects.</returns>
        public List<RoundDto> ParseReport(HtmlDocument document)
        {
            var rounds = new List<RoundDto>();

            var table = document.DocumentNode.SelectNodes("//table[@id='resultstable']");
            if (table == null)
            {
                _logger.LogError("Failed to locate rounds for member");
                return null;
            }

            // Locate the rows in the table
            var rows = document.DocumentNode.SelectNodes("//table[@id='resultstable']/tr");
            if (rows == null || rows.Count == 0)
            {
                _logger.LogWarning("No round data found in the report.");
                return rounds;
            }

            foreach (var row in rows)
            {
                var columns = row.SelectNodes(".//td")?.Select(td => td.InnerHtml.Trim()).ToArray();
                if (columns == null || columns.Length < 6) continue;

                try
                {
                    var round = new RoundDto
                    {
                        CompetitionName = ExtractCompetitionName(columns[0]),
                        CompetitionId = ExtractCompetitionId(columns[0]),
                        IsGeneralPlay = columns[0].Contains("General Play Score"),
                        IsHandicappingRound = ExtractHandicapQualifying(columns[0]),
                        DatePlayed = ParseDate(columns[1]),
                        Course = ExtractCourseName(columns[2]),
                        TeeColour = ExtractTeeColour(columns[2]),
                        RoundId = ExtractRoundId(columns[3]),
                        GrossScore = ExtractGrossScore(columns[3]),
                        NetScore = ParseNullableInt(columns[4]),
                        StablefordPoints = ParseNullableInt(columns[5])
                    };

                    rounds.Add(round);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing round row: {RowData}", string.Join(", ", columns));
                }
            }

            _logger.LogInformation("Successfully parsed {Count} rounds.", rounds.Count);
            return rounds;
        }

        /// <summary>
        /// Extracts the competition name, removing extra HTML.
        /// </summary>
        private static string ExtractCompetitionName(string html)
        {
            var name = Regex.Replace(html, "<.*?>", "").Trim();
            name = Regex.Replace(name, "H\\s*$", "").Trim();
            name = Regex.Replace(name, "\\s*-\\s*[^-]*Tees?\\s*$", "").Trim();
            name = Regex.Replace(name, "[(]\\s*[^)]*Tees?\\s*[)]\\s*$", "").Trim();
            return name;
        }


        /// <summary>
        /// Extracts whether the round was a handicap qualifying round
        /// </summary>
        private static bool ExtractHandicapQualifying(string html)
        {
            var name = Regex.Replace(html, "<.*?>", "").Trim();
            return Regex.IsMatch(name, "\\sH\\s*$", RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Extracts the competition ID from the hyperlink if available.
        /// </summary>
        private static int? ExtractCompetitionId(string html)
        {
            var match = Regex.Match(html, @"competition\.php\?compid=(\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : null;
        }

        /// <summary>
        /// Extracts the course name from the column.
        /// </summary>
        private static string ExtractCourseName(string html)
        {
            return Regex.Replace(html, "<.*?>", "").Trim();
        }

        /// <summary>
        /// Extracts the tee colour from the icon in the course column.
        /// </summary>
        private static string ExtractTeeColour(string html)
        {
            var match = Regex.Match(html, @"^.*?(White|Yellow|Red|Green|Black)\\s*Tee.*?$", RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.ToUpper() : "Unknown";
        }

        /// <summary>
        /// Extracts the round ID from the "Gross" column if available.
        /// </summary>
        private static int ExtractRoundId(string html)
        {
            var match = Regex.Match(html, @"viewround\.php\?roundid=(\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : 0;
        }

        /// <summary>
        /// Extracts the gross score, handling cases where additional text exists (e.g., "+ 9 holes").
        /// </summary>
        private static string ExtractGrossScore(string html)
        {
            var match = Regex.Match(html, @">(\d+\s*(\+\s*\d+\s*holes)?)<");
            return match.Success ? match.Groups[1].Value.Trim() : "NR";
        }

        /// <summary>
        /// Parses an integer from a table cell, returning null if the value is empty.
        /// </summary>
        private static int? ParseNullableInt(string text)
        {
            return int.TryParse(text, out var value) ? value : (int?)null;
        }

        /// <summary>
        /// Parses a date from the report, handling multiple formats.
        /// </summary>
        private static DateTime ParseDate(string text)
        {
            text = Regex.Replace(text, @"(\d+)(st|nd|rd|th)", "$1");
            text = Regex.Replace(text, "\\s{2,}", " ");
            if (DateTime.TryParseExact(text, "dddd d MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                return date;
            }

            throw new FormatException($"Invalid date format: {text}");
        }
    }
}
