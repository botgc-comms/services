using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using global::Services.Dto;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using Services.Interfaces;

namespace Services.Common
{
    public class IGPlayerIdLookupReportParser: IReportParser<PlayerIdLookupDto>
    {
        private readonly ILogger<IGPlayerIdLookupReportParser> _logger;

        public IGPlayerIdLookupReportParser(ILogger<IGPlayerIdLookupReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<PlayerIdLookupDto> ParseReport(HtmlDocument document)
        {
            var playerIdsLookup = new List<PlayerIdLookupDto>();

            var activeMembers = document.DocumentNode.SelectSingleNode("//fieldset[@id='memberlist']");
            if (activeMembers == null)
            {
                _logger.LogWarning("Unable to locate active members in report.");
                return playerIdsLookup;
            }

            var rows = activeMembers.SelectNodes(".//tr");
            if (rows == null || rows.Count < 2)
            {
                _logger.LogWarning("No active member rows found in the report.");
                return playerIdsLookup;
            }

            // Extract headers from the first row
            var headers = activeMembers.SelectNodes(".//th")?.Select(th => th.InnerText.Trim()).ToArray();
            if (headers == null || headers.Length == 0)
            {
                _logger.LogError("Failed to extract headers from the report.");
                return playerIdsLookup;
            }

            _logger.LogInformation("Extracted {Count} headers: {Headers}", headers.Length, string.Join(", ", headers));

            // Define regex patterns to match headers with DTO properties
            var columnMapping = new Dictionary<string, string>
            {
                { "^Forename$", nameof(PlayerIdLookupDto.PlayerId) },
                { "^Member\\sID$", nameof(PlayerIdLookupDto.MemberId) },
            };

            // Build a dictionary to track the index of each column
            var headerIndexMap = new Dictionary<string, int>();

            foreach (var header in headers.Select((text, index) => new { text, index }))
            {
                foreach (var pattern in columnMapping.Keys)
                {
                    if (Regex.IsMatch(header.text, pattern, RegexOptions.IgnoreCase))
                    {
                        headerIndexMap[columnMapping[pattern]] = header.index;
                        break; // Stop checking once a match is found
                    }
                }
            }

            _logger.LogInformation("Mapped {Count} columns: {Mapping}", headerIndexMap.Count, string.Join(", ", headerIndexMap));

            // Process data rows
            foreach (var row in rows.Skip(1))
            {
                var columns = row.SelectNodes(".//td")?.Select(td => td.InnerHtml.Trim()).ToArray();
                if (columns == null || columns.Length == 0) continue;

                try
                {
                    var playerIdLookup = new PlayerIdLookupDto();

                    if (headerIndexMap.TryGetValue(nameof(PlayerIdLookupDto.PlayerId), out var playerIdIndex) && playerIdIndex < columns.Length)
                        playerIdLookup.PlayerId = ExtractPlayerId(columns[playerIdIndex]) ?? 0;

                    if (headerIndexMap.TryGetValue(nameof(PlayerIdLookupDto.MemberId), out var memberIdIndex) && memberIdIndex < columns.Length)
                        playerIdLookup.MemberId = int.TryParse(columns[memberIdIndex], out var id) ? id : 0;

                    playerIdsLookup.Add(playerIdLookup);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing row: {RowData}", string.Join(", ", columns));
                }
            }

            _logger.LogInformation("Successfully parsed {Count} active members.", playerIdsLookup.Count);
            return playerIdsLookup;
        }

        /// <summary>
        /// Extracts the competition ID from the hyperlink if available.
        /// </summary>
        private static int? ExtractPlayerId(string html)
        {
            var match = Regex.Match(html, @"member\.php\?memberid=(\d+)");
            return match.Success ? int.Parse(match.Groups[1].Value) : null;
        }
    }
}
