using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using HtmlAgilityPack;
using Services.Interfaces;
using Services.Dto;
using Microsoft.Extensions.Options;
using System.Runtime;
using Services.Common;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Html;

namespace Services.Services
{
    public class IGReportsService : IReportService
    {
        private readonly AppSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<IGReportsService> _logger;

        private readonly IGLoginService _loginService;
        private readonly IGMemberReportParser _memberReportParser;

        public IGReportsService(IOptions<AppSettings> settings,
                                ILogger<IGReportsService> logger,
                                IGLoginService loginService,
                                IGMemberReportParser memberReportParser,
                                HttpClient httpClient)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
            _memberReportParser = memberReportParser ?? throw new ArgumentNullException(nameof(memberReportParser));
        }

        public async Task<List<MemberDto>> GetJuniorMembersAsync()
        {
            var members = await GetMembers($"{_settings.IG.BaseUrl}{_settings.IG.IGReports.JuniorMembershipReportUrl}");

            var now = DateTime.UtcNow;
            var cutoffDate = new DateTime(now.Year, 1, 1).AddYears(-18); // 1st January of the current year - 18 years

            var juniorMembers = members
                .Where(m => m.IsActive // Active members only
                    && (!m.LeaveDate.HasValue || m.LeaveDate.Value >= now) // No past leave date
                    && m.DateOfBirth.HasValue && m.DateOfBirth.Value >= cutoffDate) // Was 18 or younger on 1st Jan
                .ToList();

            _logger.LogInformation("Filtered {Count} junior members from {Total} total members.", juniorMembers.Count, members.Count);

            return juniorMembers;
        }

        private async Task<List<MemberDto>> GetMembers(string reportUrl)
        { 
            _logger.LogInformation("Starting member report retrieval...");

            // Step 1: Log in
            if (!await _loginService.LoginAsync())
            {
                _logger.LogError("Failed to log in. Cannot fetch member report.");
                return new List<MemberDto>();
            }

            // Step 2: Fetch the members report page
            _logger.LogInformation("Fetching members report from {Url}", reportUrl);
            var response = await _httpClient.GetAsync(reportUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch member report. Status: {StatusCode}", response.StatusCode);
                return new List<MemberDto>();
            }

            var pageContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Successfully retrieved report data.");

            var doc = new HtmlDocument();
            doc.LoadHtml(pageContent);

            var members = _memberReportParser.ParseReport(doc);

            // Step 3: Parse HTML and extract table data
            return members;
        }
    }
}
