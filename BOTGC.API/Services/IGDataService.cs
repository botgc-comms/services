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
using BOTGC.API.Services;

namespace Services.Services
{
    public class IGDataService : IDataService
    {
        private const string __CACHE_JUNIORMEMBERS = "Junior_Members";
        private const string __CACHE_PLAYERIDLOOKUP = "PlayerId_Lookup";
        private const string __CACHE_ROUNDSBYMEMBER = "Rounds_By_Member_{memberId}";
        private const string __CACHE_SCORECARDBYROUND = "Scorecard_By_Round_{roundId}";

        private readonly AppSettings _settings;
        private readonly ILogger<IGDataService> _logger;
        private IServiceScopeFactory _serviceScopeFactory;
        private readonly IGSessionService _igSessionManagementService;
        
        private readonly IReportParser<MemberDto> _memberReportParser;
        private readonly IReportParser<RoundDto> _roundReportParser;
        private readonly IReportParser<PlayerIdLookupDto> _playerIdLookupParser;
        private readonly IReportParser<ScorecardDto> _scorecardReportParser;

        public IGDataService(IOptions<AppSettings> settings,
                                ILogger<IGDataService> logger,
                                IReportParser<MemberDto> memberReportParser,
                                IReportParser<RoundDto> roundReportParser,
                                IReportParser<PlayerIdLookupDto> playerIdLookupParser,
                                IReportParser<ScorecardDto> scorecardReportParser,
                                IGSessionService igSessionManagementService,
                                IServiceScopeFactory serviceScopeFactory)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

            _igSessionManagementService = igSessionManagementService ?? throw new ArgumentNullException(nameof(igSessionManagementService));
            _memberReportParser = memberReportParser ?? throw new ArgumentNullException(nameof(memberReportParser));
            _roundReportParser = roundReportParser ?? throw new ArgumentNullException(nameof(roundReportParser));
            _playerIdLookupParser = playerIdLookupParser ?? throw new ArgumentNullException(nameof(playerIdLookupParser));
            _scorecardReportParser = scorecardReportParser ?? throw new ArgumentNullException(nameof(scorecardReportParser));
        }
        
        public async Task<List<MemberDto>> GetJuniorMembersAsync()
        {
            var playerIdLookup = await GetPlayerIdsByMemberAsync();

            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.IGReports.JuniorMembershipReportUrl}";
            var members = await GetReportData<MemberDto>(reportUrl, _memberReportParser, __CACHE_JUNIORMEMBERS, TimeSpan.FromMinutes(_settings.Cache.ShortTerm_TTL_mins), HateOASLinks.GetMemberLinks);

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

        public async Task<List<RoundDto>> GetRoundsByMemberIdAsync(string memberId)
        {
            var cacheKey = __CACHE_ROUNDSBYMEMBER.Replace("{memberId}", memberId);

            var playerLookupData = await GetPlayerIdsByMemberAsync();
            var playerLookupId = playerLookupData.Where(id => id.MemberId.ToString() == memberId).SingleOrDefault();

            if (playerLookupId == null)
            {
                _logger.LogWarning($"Failed to lookup player id for member {memberId}");
                throw new KeyNotFoundException($"No player found for member ID {memberId}");
            }
            
            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.IGReports.MemberRoundsReportUrl.Replace("{playerId}", playerLookupId.PlayerId.ToString())}";
            var memberRounds = await GetReportData<RoundDto>(reportUrl, _roundReportParser, cacheKey, TimeSpan.FromMinutes(_settings.Cache.ShortTerm_TTL_mins), HateOASLinks.GetRoundLinks);

            _logger.LogInformation($"Retrieved {memberRounds.Count()} rounds for member {memberId}");

            return memberRounds;
        }

        public async Task<List<PlayerIdLookupDto>> GetPlayerIdsByMemberAsync()
        {
            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.IGReports.PlayerIdLookupReportUrl}";
            var playerIdLookup = await GetReportData<PlayerIdLookupDto>(reportUrl, _playerIdLookupParser, __CACHE_PLAYERIDLOOKUP);

            _logger.LogInformation($"Retrieved {playerIdLookup.Count()} player lookup records.");

            return playerIdLookup;
        }

        public async Task<ScorecardDto?> GetScorecardForRoundAsync(string roundId)
        {
            var cacheKey = __CACHE_SCORECARDBYROUND.Replace("{roundId}", roundId);

            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.IGReports.RoundReportUrl.Replace("{roundId}", roundId)}";
            var scorecards = await GetReportData<ScorecardDto>(reportUrl, _scorecardReportParser, cacheKey, TimeSpan.FromMinutes(_settings.Cache.LongTerm_TTL_mins));

            if (scorecards != null && scorecards.Any())
            {
                _logger.LogInformation($"Successfully retrieved the scorecard for round {roundId}.");

                return scorecards.FirstOrDefault();
            }

            return null;
        }

        private async Task<List<T>> GetReportData<T>(string reportUrl,
                                                     IReportParser<T> parser,
                                                     string? cacheKey = null,
                                                     TimeSpan? cacheTTL = null, 
                                                     Func<T, List<HateoasLink>>? linkBuilder = null) where T : HateoasResource, new()
        {
            ICacheService? cacheService = null;

            if (!String.IsNullOrEmpty(cacheKey))
            {
                using var scope = _serviceScopeFactory.CreateScope();
                cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

                var cachedResults = await cacheService!.GetAsync<List<T>>(cacheKey).ConfigureAwait(false);
                if (cachedResults != null && cachedResults.Any())
                {
                    _logger.LogInformation("Retrieving results from cache for {ReportType}...", typeof(T).Name);
                    return cachedResults;
                }
            }

            _logger.LogInformation("Starting report retrieval for {ReportType}...", typeof(T).Name);

            // Step 1: Log in
            await _igSessionManagementService.WaitForLoginAsync();

            // Step 2: Fetch the report page
            _logger.LogInformation("Fetching {ReportType} report from {Url}", typeof(T).Name, reportUrl);
            var doc = await _igSessionManagementService.GetPageContent(reportUrl);

            var items = parser.ParseReport(doc);

            // Step 4: Add Hateoas Links
            if (linkBuilder != null)
            {
                foreach (var item in items)
                {
                    item.Links = linkBuilder(item);
                }
            }

            if (cacheService != null && items != null)
            {
                await cacheService.SetAsync(cacheKey!, items, cacheTTL!.Value).ConfigureAwait(false);
            }

            return items;
        }
    }
}
