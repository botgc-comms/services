using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using BOTGC.API.Common;
using HtmlAgilityPack;
using Microsoft.Extensions.Logging;
using BOTGC.API.Interfaces;
using BOTGC.API.Dto;
using System.Globalization;

namespace BOTGC.API.IGScrapers
{
    public class IGMemberReportParser : IReportParser<MemberDto>
    {
        private readonly ILogger<IGMemberReportParser> _logger;

        public IGMemberReportParser(ILogger<IGMemberReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<MemberDto>> ParseReport(HtmlDocument document)
        {
            var members = new List<MemberDto>();

            // Get all rows in the table
            var rows = document.DocumentNode.SelectNodes("//tr");
            if (rows == null || rows.Count < 2)
            {
                _logger.LogWarning("No data rows found in the report.");
                return members;
            }

            // Extract headers from the first row
            var headers = rows[0].SelectNodes(".//th")?.Select(th => th.InnerText.Trim()).ToArray();
            if (headers == null || headers.Length == 0)
            {
                _logger.LogError("Failed to extract headers from the report.");
                return members;
            }

            _logger.LogInformation("Extracted {Count} headers: {Headers}", headers.Length, string.Join(", ", headers));

            // Define regex patterns to match headers with DTO properties
            var columnMapping = new Dictionary<string, string>
            {
                { "^(?:Member|Account)\\s*.*?Number$", nameof(MemberDto.MemberNumber) },
                { "^Title$", nameof(MemberDto.Title) },
                { "^Forename$", nameof(MemberDto.FirstName) },
                { "^Surname$", nameof(MemberDto.LastName) },
                { "^Full\\s*Name$", nameof(MemberDto.FullName) },
                { "^Gender$", nameof(MemberDto.Gender) },
                { "^Current\\s*Category$", nameof(MemberDto.MembershipCategory) },
                { "^Membership\\s*Status$", nameof(MemberDto.MembershipStatus) },
                { "^Address\\s*1$", nameof(MemberDto.Address1) },
                { "^Address\\s*2$", nameof(MemberDto.Address2) },
                { "^Address\\s*3$", nameof(MemberDto.Address3) },
                { "^Town$", nameof(MemberDto.Town) },
                { "^County$", nameof(MemberDto.County) },
                { "^Postcode$", nameof(MemberDto.Postcode) },
                { "^Email$", nameof(MemberDto.Email) },
                { "^Dob$", nameof(MemberDto.DateOfBirth) },
                { "^Join\\s*Date$", nameof(MemberDto.JoinDate) },
                { "^Leave\\s*Date$", nameof(MemberDto.LeaveDate) },
                { "^Application\\s*Date$", nameof(MemberDto.ApplicationDate) },
                { "^Handicap$", nameof(MemberDto.Handicap) },
                { "^Disabled\\s*Golfer$", nameof(MemberDto.IsDisabledGolfer) },
                { "^Unpaid\\s*Total$", nameof(MemberDto.UnpaidTotal) },
                { "^ApplicationID$", nameof(MemberDto.ApplicationId) },
                { "^ReferrerId$", nameof(MemberDto.ReferrerId) }
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
                    var member = new MemberDto();

                    if (headerIndexMap.TryGetValue(nameof(MemberDto.MemberNumber), out var memberIdIndex) && memberIdIndex < columns.Length)
                        member.MemberNumber = int.TryParse(columns[memberIdIndex], out var id) ? id : 0;

                    if (headerIndexMap.TryGetValue(nameof(MemberDto.Title), out var titleIndex) && titleIndex < columns.Length)
                        member.Title = columns[titleIndex];

                    if (headerIndexMap.TryGetValue(nameof(MemberDto.FirstName), out var firstNameIndex) && firstNameIndex < columns.Length)
                        member.FirstName = columns[firstNameIndex];

                    if (headerIndexMap.TryGetValue(nameof(MemberDto.LastName), out var lastNameIndex) && lastNameIndex < columns.Length)
                        member.LastName = columns[lastNameIndex];

                    if (headerIndexMap.TryGetValue(nameof(MemberDto.FullName), out var fullNameIndex) && fullNameIndex < columns.Length)
                        member.FullName = columns[fullNameIndex];

                    if (headerIndexMap.TryGetValue(nameof(MemberDto.Gender), out var genderIndex) && genderIndex < columns.Length)
                        member.Gender = columns[genderIndex];

                    if (headerIndexMap.TryGetValue(nameof(MemberDto.MembershipCategory), out var categoryIndex) && categoryIndex < columns.Length)
                        member.MembershipCategory = columns[categoryIndex];

                    if (headerIndexMap.TryGetValue(nameof(MemberDto.MembershipStatus), out var statusIndex) && statusIndex < columns.Length)
                        member.MembershipStatus = columns[statusIndex];

                    if (headerIndexMap.TryGetValue(nameof(MemberDto.Email), out var emailIndex) && emailIndex < columns.Length)
                        member.Email = columns[emailIndex];

                    if (headerIndexMap.TryGetValue(nameof(MemberDto.DateOfBirth), out var dobIndex) && dobIndex < columns.Length)
                        member.DateOfBirth = ParseDate(columns[dobIndex]);

                    if (headerIndexMap.TryGetValue(nameof(MemberDto.JoinDate), out var joinIndex) && joinIndex < columns.Length)
                        member.JoinDate = ParseDate(columns[joinIndex]);

                    if (headerIndexMap.TryGetValue(nameof(MemberDto.LeaveDate), out var leaveIndex) && leaveIndex < columns.Length)
                        member.LeaveDate = ParseDate(columns[leaveIndex]);

                    if (headerIndexMap.TryGetValue(nameof(MemberDto.Postcode), out var postcodeIndex) && postcodeIndex < columns.Length)
                        member.Postcode = columns[postcodeIndex];

                    if (headerIndexMap.TryGetValue(nameof(MemberDto.ApplicationDate), out var applicationDateIndex) && applicationDateIndex < columns.Length)
                        member.ApplicationDate = ParseDate(columns[applicationDateIndex]);

                    if (headerIndexMap.TryGetValue(nameof(MemberDto.Handicap), out var handicapIndex) && handicapIndex < columns.Length)
                        member.Handicap = columns[handicapIndex];

                    if (headerIndexMap.TryGetValue(nameof(MemberDto.IsDisabledGolfer), out var disabledIndex) && disabledIndex < columns.Length)
                        member.IsDisabledGolfer = columns[disabledIndex].Equals("Yes", StringComparison.OrdinalIgnoreCase);

                    if (headerIndexMap.TryGetValue(nameof(MemberDto.UnpaidTotal), out var unpaidIndex) && unpaidIndex < columns.Length)
                        member.UnpaidTotal = decimal.TryParse(columns[unpaidIndex], out var unpaid) ? unpaid : 0;

                    if (headerIndexMap.TryGetValue(nameof(MemberDto.ApplicationId), out var applicationIdIndex) && applicationIdIndex < columns.Length)
                        member.ApplicationId = columns[applicationIdIndex];

                    if (headerIndexMap.TryGetValue(nameof(MemberDto.ReferrerId), out var referrerIdIndex) && referrerIdIndex < columns.Length)
                        member.ReferrerId = columns[referrerIdIndex];

                    // "R" means Active
                    member.IsActive = member.MembershipStatus.Equals("R", StringComparison.OrdinalIgnoreCase);

                    // Set the primary membership category
                    member.SetPrimaryCategory().SetCategoryGroup();

                    members.Add(member);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing row: {RowData}", string.Join(", ", columns));
                }
            }

            _logger.LogInformation("Successfully parsed {Count} members.", members.Count);
            return members;
        }

        private static DateTime? ParseDate(string? s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;

            s = s.Trim();

            var gb = CultureInfo.GetCultureInfo("en-GB");

            var formats = new[]
            {
                "dd/MM/yyyy", "d/M/yyyy", "dd/MM/yy", "d/M/yy",
                "yyyy-MM-dd", "yyyy-M-d", "yyyy/MM/dd", "yyyy/M/d"
            };

            if (DateTime.TryParseExact(s, formats, gb, DateTimeStyles.None, out var dt))
                return dt;

            if (DateTime.TryParse(s, gb, DateTimeStyles.None, out dt))
                return dt;

            return null;
        }
    }

}
