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
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.InteropServices;

namespace Services.Services
{
    public class IGReportsService : IReportService
    {
        private const string __CACHE_JUNIORMEMBERS = "Junior_Members";
        private const string __CACHE_PLAYERIDLOOKUP = "PlayerId_Lookup";
        private const string __CACHE_ROUNDSBYMEMBER = "Rounds_By_Member_{memberId}";

        private readonly AppSettings _settings;
        private readonly HttpClient _httpClient;
        private readonly ILogger<IGReportsService> _logger;
        private IServiceScopeFactory _serviceScopeFactory;

        private readonly IGLoginService _loginService;

        private readonly IReportParser<MemberDto> _memberReportParser;
        private readonly IReportParser<RoundDto> _roundReportParser;
        private readonly IReportParser<PlayerIdLookupDto> _playerIdLookupParser;
        
        public IGReportsService(IOptions<AppSettings> settings,
                                ILogger<IGReportsService> logger,
                                IGLoginService loginService,
                                IReportParser<MemberDto> memberReportParser,
                                IReportParser<RoundDto> roundReportParser,
                                IReportParser<PlayerIdLookupDto> playerIdLookupParser,
                                HttpClient httpClient,
                                IServiceScopeFactory serviceScopeFactory)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

            _loginService = loginService ?? throw new ArgumentNullException(nameof(loginService));
            _memberReportParser = memberReportParser ?? throw new ArgumentNullException(nameof(memberReportParser));
            _roundReportParser = roundReportParser ?? throw new ArgumentNullException(nameof(roundReportParser));
            _playerIdLookupParser = playerIdLookupParser ?? throw new ArgumentNullException(nameof(playerIdLookupParser));
        }
        
        public async Task<List<MemberDto>> GetJuniorMembersAsync()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var cachedJuniorMembers = await cacheService.GetAsync<List<MemberDto>>(__CACHE_JUNIORMEMBERS).ConfigureAwait(false);
            if (cachedJuniorMembers != null && cachedJuniorMembers.Any())
            {
                _logger.LogInformation($"Retrieved {cachedJuniorMembers.Count()} junior members from cache.");
                return cachedJuniorMembers;
            }

            var playerIdLookup = await GetPlayerIdsByMemberAsync();

            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.IGReports.JuniorMembershipReportUrl}";
            var members = await GetReportData<MemberDto>(reportUrl, _memberReportParser, HateOASLinks.GetMemberLinks);

            var now = DateTime.UtcNow;
            var cutoffDate = new DateTime(now.Year, 1, 1).AddYears(-18); // 1st January of the current year - 18 years

            var juniorMembers = members
                .Where(m => m.IsActive // Active members only
                    && (!m.LeaveDate.HasValue || m.LeaveDate.Value >= now) // No past leave date
                    && m.DateOfBirth.HasValue && m.DateOfBirth.Value >= cutoffDate) // Was 18 or younger on 1st Jan
                .ToList();

            _logger.LogInformation("Filtered {Count} junior members from {Total} total members.", juniorMembers.Count, members.Count);

            await cacheService.SetAsync(__CACHE_JUNIORMEMBERS, juniorMembers, TimeSpan.FromMinutes(_settings.Cache.TTL_mins)).ConfigureAwait(false);

            return juniorMembers;
        }

        public async Task<List<RoundDto>> GetRoundsByMemberIdAsync(string memberId)
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var cacheKey = __CACHE_ROUNDSBYMEMBER.Replace("{memberId}", memberId);

            // Attempt to retrieve from cache
            var cachedRounds = await cacheService.GetAsync<List<RoundDto>>(cacheKey).ConfigureAwait(false);
            if (cachedRounds != null && cachedRounds.Any())
            {
                _logger.LogInformation($"Retrieved {cachedRounds.Count()} rounds for member {memberId} from cache.");
                return cachedRounds;
            }

            var playerLookupData = await GetPlayerIdsByMemberAsync();
            var playerLookupId = playerLookupData.Where(id => id.MemberId.ToString() == memberId).SingleOrDefault();

            if (playerLookupId == null)
            {
                _logger.LogWarning($"Failed to lookup player id for member {memberId}");
                throw new KeyNotFoundException($"No player found for member ID {memberId}");
            }
            
            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.IGReports.MemberRoundsReportUrl.Replace("{playerId}", playerLookupId.PlayerId.ToString())}";
            var memberRounds = await GetReportData<RoundDto>(reportUrl, _roundReportParser, HateOASLinks.GetRoundLinks);

            _logger.LogInformation($"Retrieved {memberRounds.Count()} rounds for member {memberId}");

            await cacheService.SetAsync(cacheKey, memberRounds, TimeSpan.FromMinutes(_settings.Cache.TTL_mins)).ConfigureAwait(false);

            return memberRounds;
        }

        public async Task<List<PlayerIdLookupDto>> GetPlayerIdsByMemberAsync()
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            // Attempt to retrieve from cache
            var cachedPlayerIdLookup = await cacheService.GetAsync<List<PlayerIdLookupDto>>(__CACHE_PLAYERIDLOOKUP).ConfigureAwait(false);
            if (cachedPlayerIdLookup != null && cachedPlayerIdLookup.Any())
            {
                _logger.LogInformation("Player ID lookup data retrieved from cache.");
                return cachedPlayerIdLookup;
            }

            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.IGReports.PlayerIdLookupReportUrl}";
            var playerIdLookup = await GetReportData<PlayerIdLookupDto>(reportUrl, _playerIdLookupParser);

            _logger.LogInformation($"Retrieved {playerIdLookup.Count()} player lookup records.");

            await cacheService.SetAsync(__CACHE_PLAYERIDLOOKUP, cachedPlayerIdLookup, TimeSpan.FromMinutes(_settings.Cache.TTL_mins)).ConfigureAwait(false);

            return playerIdLookup;
        }

        private async Task<List<T>> GetReportData<T>(string reportUrl, IReportParser<T> parser, Func<T, List<HateoasLink>>? linkBuilder = null) where T : HateoasResource, new()
        {
            _logger.LogInformation("Starting report retrieval for {ReportType}...", typeof(T).Name);

            // Step 1: Log in
            if (!await _loginService.LoginAsync())
            {
                _logger.LogError("Failed to log in. Cannot fetch {ReportType} report.", typeof(T).Name);
                return new List<T>();
            }

            // Step 2: Fetch the report page
            _logger.LogInformation("Fetching {ReportType} report from {Url}", typeof(T).Name, reportUrl);
            var response = await _httpClient.GetAsync(reportUrl);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Failed to fetch {ReportType} report. Status: {StatusCode}", typeof(T).Name, response.StatusCode);
                return new List<T>();
            }

            // Step 3: Parse the content of the report
            var pageContent = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("Successfully retrieved {ReportType} report data.", typeof(T).Name);

            var doc = new HtmlDocument();
            doc.LoadHtml(pageContent);

            var items = parser.ParseReport(doc);

            // Step 4: Add Hateoas Links
            if (linkBuilder != null)
            {
                foreach (var item in items)
                {
                    item.Links = linkBuilder(item);
                }
            }

            return items;
        }
    }
}
