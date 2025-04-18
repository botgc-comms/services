using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using BOTGC.API.Interfaces;
using BOTGC.API.Dto;

namespace BOTGC.API.Common
{
    public class IGSecurityLogReportParser: IReportParser<SecurityLogEntryDto>
    {
        private readonly ILogger<IGSecurityLogReportParser> _logger;

        public IGSecurityLogReportParser(ILogger<IGSecurityLogReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public List<SecurityLogEntryDto> ParseReport(HtmlDocument document)
        {
            var events = new List<SecurityLogEntryDto>();

            // Get all rows in the table
            var rows = document.DocumentNode.SelectNodes("//tr");
            if (rows == null || rows.Count < 2)
            {
                _logger.LogWarning("No data rows found in the report.");
                return events;
            }

            // Extract headers from the first row
            var headers = rows[0].SelectNodes(".//th")?.Select(th => th.InnerText.Trim()).ToArray();
            if (headers == null || headers.Length == 0)
            {
                _logger.LogError("Failed to extract headers from the report.");
                return events;
            }

            _logger.LogInformation("Extracted {Count} headers: {Headers}", headers.Length, string.Join(", ", headers));

            // Define regex patterns to match headers with DTO properties
            var columnMapping = new Dictionary<string, string>
            {
                { "^Date$", "Date" },
                { "^Time$", "Time" },
                { "^Event$", nameof(SecurityLogEntryDto.Event) },
                { "^Admin$", nameof(SecurityLogEntryDto.Admin) },
                { "^Subject$", nameof(SecurityLogEntryDto.Subject) },
                { "^IP$", nameof(SecurityLogEntryDto.IP) },
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
                var columns = row.SelectNodes(".//td")?.Select(td => td.InnerText.Trim()).ToArray();
                if (columns == null || columns.Length == 0) continue;

                try
                {
                    var entry = new SecurityLogEntryDto();

                    if ((headerIndexMap.TryGetValue("Date", out var eventDateIdx) && eventDateIdx < columns.Length) &&
                        (headerIndexMap.TryGetValue("Time", out var eventTimeIdx) && eventTimeIdx < columns.Length))
                    {
                        entry.OccuredAt = ParseDate($"{columns[eventDateIdx]} {columns[eventTimeIdx]}");
                    }

                    if (headerIndexMap.TryGetValue(nameof(SecurityLogEntryDto.Event), out var eventIndex) && eventIndex < columns.Length)
                        entry.Event = columns[eventIndex];

                    if (headerIndexMap.TryGetValue(nameof(SecurityLogEntryDto.Admin), out var adminIndex) && adminIndex < columns.Length)
                        entry.Admin = columns[adminIndex];

                    if (headerIndexMap.TryGetValue(nameof(SecurityLogEntryDto.Subject), out var subjectIndex) && subjectIndex < columns.Length)
                        entry.Subject = columns[subjectIndex];

                    if (headerIndexMap.TryGetValue(nameof(SecurityLogEntryDto.IP), out var ipIndex) && ipIndex < columns.Length)
                        entry.IP = columns[ipIndex];

                    events.Add(entry);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing row: {RowData}", string.Join(", ", columns));
                }
            }

            _logger.LogInformation("Successfully parsed {Count} events.", events.Count);
            return events;
        }

        private DateTime? ParseDate(string date)
        {
            // Replace broken 24-hour+AM/PM combo with correct 24-hour time
            date = Regex.Replace(date, @"(\d{2}-\d{2}-\d{4}) (\d{2}:\d{2}:\d{2}) (AM|PM)", "$1 $2");

            if (DateTime.TryParseExact(date, "dd-MM-yyyy HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
            {
                return parsedDate;
            }

            return null;
        }
    }
}
