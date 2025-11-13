using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace BOTGC.API.IGScrapers
{
    public class IGMemberEventsReportParser : IReportParser<MemberEventDto>
    {
        private readonly ILogger<IGMemberEventsReportParser> _logger;

        public IGMemberEventsReportParser(ILogger<IGMemberEventsReportParser> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<List<MemberEventDto>> ParseReport(HtmlDocument document)
        {
            var memberEvents = new List<MemberEventDto>();

            // Get all rows in the table
            var rows = document.DocumentNode.SelectNodes("//tbody//tr");
            if (rows == null || rows.Count < 2)
            {
                _logger.LogWarning("No data rows found in the report.");
                return memberEvents;
            }

            // Extract headers from the first row
            var headers = document.DocumentNode.SelectNodes("//thead/th")?.Select(th => th.InnerText.Trim()).ToArray();
            if (headers == null || headers.Length == 0)
            {
                _logger.LogError("Failed to extract headers from the report.");
                return memberEvents;
            }

            _logger.LogInformation("Extracted {Count} headers: {Headers}", headers.Length, string.Join(", ", headers));

            // Define regex patterns to match headers with DTO properties
            var columnMapping = new Dictionary<string, string>
            {
                { "^Forename$", nameof(MemberEventDto.Forename) },
                { "^Surname$", nameof(MemberEventDto.Surname) },
                { "^Surname\\s*?$", nameof(MemberEventDto.AccountId) },
                { "^Date\\s*of\\s*Change$", nameof(MemberEventDto.DateOfChange) },
                { "^Date\\s*Created$", nameof(MemberEventDto.DateCreated) },
                { "^From\\s*Category$", nameof(MemberEventDto.FromCategory) },
                { "^To\\s*Category$", nameof(MemberEventDto.ToCategory) },
                { "^From\\s*Status$", nameof(MemberEventDto.FromStatus) },
                { "^To\\s*Status$", nameof(MemberEventDto.ToStatus) },
                { "^Membership\\s*ID", nameof(MemberEventDto.MemberId) },
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
            for (var rowIdx = 0; rowIdx < rows.Count; rowIdx++)
            {
                var row = rows[rowIdx];
                ;
                var columns = row.SelectNodes(".//td")?.Select(td => new { text = td.InnerText.Trim(), html = td.InnerHtml.Trim() }).ToArray();
                if (columns == null || columns.Length == 0) continue;

                try
                {
                    var memberEvent = new MemberEventDto()
                    {
                        ChangeIndex = rowIdx
                    };

                    if (headerIndexMap.TryGetValue(nameof(MemberEventDto.Forename), out var forenameIndex) && forenameIndex < columns.Length)
                        memberEvent.Forename = columns[forenameIndex].text;

                    if (headerIndexMap.TryGetValue(nameof(MemberEventDto.Surname), out var surenameIndex) && surenameIndex < columns.Length)
                        memberEvent.Surname = columns[surenameIndex].text;

                    if (headerIndexMap.TryGetValue(nameof(MemberEventDto.AccountId), out var accountNumberIndex) && accountNumberIndex < columns.Length)
                        memberEvent.AccountId = ParseAccountId(columns[accountNumberIndex].html);

                    if (headerIndexMap.TryGetValue(nameof(MemberEventDto.MemberId), out var memberIdIndex) && memberIdIndex < columns.Length)
                        memberEvent.MemberId = int.TryParse(columns[memberIdIndex].text, out var id) ? id : 0;

                    if (headerIndexMap.TryGetValue(nameof(MemberEventDto.DateOfChange), out var dateOfChangeIndex) && dateOfChangeIndex < columns.Length)
                        memberEvent.DateOfChange = ParseDate(columns[dateOfChangeIndex].text)
;
                    if (headerIndexMap.TryGetValue(nameof(MemberEventDto.DateCreated), out var dateCreaatedIndex) && dateCreaatedIndex < columns.Length)
                        memberEvent.DateCreated = ParseDate(columns[dateCreaatedIndex].text);

                    if (headerIndexMap.TryGetValue(nameof(MemberEventDto.FromCategory), out var fromCatIndex) && fromCatIndex < columns.Length)
                        memberEvent.FromCategory = columns[fromCatIndex].text;

                    if (headerIndexMap.TryGetValue(nameof(MemberEventDto.ToCategory), out var toCatIndex) && toCatIndex < columns.Length)
                        memberEvent.ToCategory = columns[toCatIndex].text;

                    if (headerIndexMap.TryGetValue(nameof(MemberEventDto.FromStatus), out var fromStatusIndex) && fromStatusIndex < columns.Length)
                        memberEvent.FromStatus = columns[fromStatusIndex].text;

                    if (headerIndexMap.TryGetValue(nameof(MemberEventDto.ToStatus), out var toStatusIndex) && toStatusIndex < columns.Length)
                        memberEvent.ToStatus = columns[toStatusIndex].text;

                    memberEvents.Add(memberEvent);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing row: {RowData}", string.Join(", ", columns.Select(c => c.text)));
                }
            }

            var testAccounts = memberEvents.Where(me => me.FromCategory.ToLower() == "tests" || me.ToCategory.ToLower() == "tests").Select(me => me.AccountId).Distinct().ToList();
            memberEvents = memberEvents.Where(me => !testAccounts.Any(accountId => accountId == me.AccountId)).Where(me => me.AccountId != 0 && !string.IsNullOrEmpty(me.Forename) && !string.IsNullOrEmpty(me.Surname)).ToList();

            _logger.LogInformation("Successfully parsed {Count} member events.", memberEvents.Count);
            return memberEvents;
        }

        private DateTime? ParseDate(string date)
        {
            if (DateTime.TryParse(date, out var parsedDate))
            {
                return parsedDate;
            }
            return null;
        }

        private int ParseAccountId(string html)
        {
            // Example: <a href="/member.php?memberid=98770">Wyatt</a>
            var match = Regex.Match(html, @"memberid=(\d+)", RegexOptions.IgnoreCase);
            if (match.Success && int.TryParse(match.Groups[1].Value, out var memberId))
            {
                return memberId;
            }
            return 0;
        }
    }
}
