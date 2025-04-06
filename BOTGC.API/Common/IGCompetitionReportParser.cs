using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Services.Dto;
using Services.Dtos;
using Services.Interfaces;

namespace Services.Common
{
    public class IGCompetitionReportParser: IReportParser<CompetitionDto>
    {
        private readonly ILogger<IGCompetitionReportParser> _logger;

        public IGCompetitionReportParser(ILogger<IGCompetitionReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Parses an HTML document and extracts round summaries.
        /// </summary>
        /// <param name="document">The HTML document containing the round table.</param>
        /// <returns>A list of parsed <see cref="RoundDto"/> objects.</returns>
        public List<CompetitionDto> ParseReport(HtmlDocument document)
        {
            var competitions = new List<CompetitionDto>();

            // Locate the rows in the table
            var rows = document.DocumentNode.SelectNodes("//tr");
            if (rows == null || rows.Count == 0)
            {
                _logger.LogWarning("No competition data found in the report.");
                return competitions;
            }

            foreach (var row in rows)
            {
                var columns = row.SelectNodes(".//td")?.Select(td => td.InnerHtml.Trim()).ToArray();
                
                try
                {
                    var competition = new CompetitionDto
                    {
                        Name = ExtractCompetitionName(columns[0]),
                        Id = ExtractCompetitionId(columns[0]),
                        Date = ParseDate(columns[1]), 
                        Gender = ExtractCompetitionGender(columns[3]),
                        AvailableForHandicaping = ExtractAvailableForHandicaping(columns[3])
                    };

                    competition.Links = HateOASLinks.GetCompetitionLinks(competition);

                    competitions.Add(competition);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing round competition: {RowData}", string.Join(", ", columns));
                }
            }

            _logger.LogInformation("Successfully parsed {Count} competitions.", competitions.Count);
            return competitions;
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
        /// Extracts the competition ID from the hyperlink if available.
        /// </summary>
        private static int? ExtractCompetitionId(string html)
        {
            var match = Regex.Match(html, @"[?&]compid=(\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : null;
        }

        /// <summary>
        /// Determines the gender category of the competition based on icon markup.
        /// </summary>
        private static Gender ExtractCompetitionGender(string html)
        {
            if (html.Contains("fa-venus-mars"))
                return Gender.Mixed;

            if (html.Contains("fa-venus"))
                return Gender.Ladies;

            if (html.Contains("fa-mars"))
                return Gender.Gents;

            return Gender.Unknown;            
        }

        /// <summary>
        /// Determines if the compeition can be used for handicapping purposes
        /// </summary>
        private static bool ExtractAvailableForHandicaping(string html)
        {
            return html.ToLower().Contains("acceptable for handicapping");
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
        private static DateTime? ParseDate(string text)
        {
            text = Regex.Replace(text, @"(\d+)(st|nd|rd|th)", "$1");
            text = Regex.Replace(text, "\\s{2,}", " ").Trim();

            var now = DateTime.Today;

            if (!Regex.IsMatch(text, @"\d{4}$"))
            {
                text += $" {now.Year}";
            }

            if (DateTime.TryParseExact(text, "dddd d MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            {
                if (date < now)
                {
                    var nextYearText = Regex.Replace(text, @"\d{4}$", (now.Year + 1).ToString());

                    if (DateTime.TryParseExact(nextYearText, "dddd d MMMM yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var nextYearDate))
                    {
                        return nextYearDate;
                    }
                }

                return date;
            }

            return null;
        }
    }
}
