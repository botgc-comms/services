using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using BOTGC.API.Interfaces;
using BOTGC.API.Dto;

namespace BOTGC.API.IGScrapers
{
    public class IGNewMembersReportParser : IReportParser<NewMemberLookupDto>
    {
        private readonly ILogger<IGNewMembersReportParser> _logger;

        public IGNewMembersReportParser(ILogger<IGNewMembersReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<NewMemberLookupDto>> ParseReport(HtmlDocument document)
        {
            var members = new List<NewMemberLookupDto>();

            // Get all rows in the table
            var rows = document.DocumentNode.SelectNodes("//tr");
            if (rows == null || rows.Count < 2)
            {
                _logger.LogWarning("No data rows found in the report.");
                return members;
            }

            // Extract headers from the first row
            var headers = document.DocumentNode.SelectNodes("//thead")[0].SelectNodes("//th")?.Select(th => th.InnerText.Trim()).ToArray();
            if (headers == null || headers.Length == 0)
            {
                _logger.LogError("Failed to extract headers from the report.");
                return members;
            }

            _logger.LogInformation("Extracted {Count} headers: {Headers}", headers.Length, string.Join(", ", headers));

            // Define regex patterns to match headers with DTO properties
            var columnMapping = new Dictionary<string, string>
            {
                { "^Forename$", nameof(NewMemberLookupDto.Forename) },
                { "^Forename.*?$", nameof(NewMemberLookupDto.PlayerId) },
                { "^Surname$", nameof(NewMemberLookupDto.Surname) },
                { "^Join\\s*Date$", nameof(NewMemberLookupDto.JoinDate) },
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
                    }
                }
            }

            _logger.LogInformation("Mapped {Count} columns: {Mapping}", headerIndexMap.Count, string.Join(", ", headerIndexMap));

            // Process data rows
            foreach (var row in rows.Skip(1))
            {
                var columns = row.SelectNodes(".//td")?.Select(td => new { text = td.InnerText.Trim(), html = td.InnerHtml.Trim() }).ToArray();
                if (columns == null || columns.Length == 0) continue;

                try
                {
                    var member = new NewMemberLookupDto();

                    if (headerIndexMap.TryGetValue(nameof(NewMemberLookupDto.PlayerId), out var memberIdIndex) && memberIdIndex < columns.Length)
                        member.PlayerId = ParseMemberNumber(columns[memberIdIndex].html) ?? 0;

                    if (headerIndexMap.TryGetValue(nameof(NewMemberLookupDto.Forename), out var firstNameIndex) && firstNameIndex < columns.Length)
                        member.Forename = columns[firstNameIndex].text;

                    if (headerIndexMap.TryGetValue(nameof(NewMemberLookupDto.Surname), out var lastNameIndex) && lastNameIndex < columns.Length)
                        member.Surname = columns[lastNameIndex].text;

                    if (headerIndexMap.TryGetValue(nameof(NewMemberLookupDto.JoinDate), out var joinDateIndex) && joinDateIndex < columns.Length)
                        member.JoinDate = ParseDate(columns[joinDateIndex].text);

                    members.Add(member);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing row: {RowData}", string.Join(", ", columns.Select(c => c.html)));
                }
            }

            _logger.LogInformation("Successfully parsed {Count} new members.", members.Count);
            return members;
        }

        private int? ParseMemberNumber(string html)
        {
            var match = Regex.Match(html, @"memberid=(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var memberId))
            {
                return memberId;
            }

            return null;
        }

        private DateTime? ParseDate(string date)
        {
            if (DateTime.TryParse(date, out var parsedDate))
            {
                return parsedDate;
            }
            return null;
        }
    }

}
