using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Extensions;
using System.Collections.Specialized;
using System.Net.Http;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace BOTGC.API.Services
{
    public class IGDataService : IDataService
    {
        private const string __CACHE_JUNIORMEMBERS = "Junior_Members";
        private const string __CACHE_CURRENTMEMBERS = "Current_Members";
        private const string __CACHE_PLAYERIDLOOKUP = "PlayerId_Lookup";
        private const string __CACHE_ROUNDSBYMEMBER = "Rounds_By_Member_{memberId}";
        private const string __CACHE_SCORECARDBYROUND = "Scorecard_By_Round_{roundId}";
        private const string __CACHE_MEMBERREPORT = "Membership_Report";
        private const string __CACHE_MEMBEREVENTHISTORYREPORT = "Membership_Event_History_{fromDate}_{toDate}";
        private const string __CACHE_TEESHEET = "TeeSheet_By_Date_{date}";
        private const string __CACHE_ACTIVECOMPETITIONS= "Active_Competitions";
        private const string __CACHE_FUTURECOMPETITIONS = "Future_Competitions";
        private const string __CACHE_COMPETITIONSETTINGS = "Competition_Settings";
        private const string __CACHE_LEADERBOARD = "Leaderboard_Settings";
        private const string __CACHE_MEMBERCDHLOOKUP = "MemberCDHLookup_{cdhid}";

        private readonly AppSettings _settings;
        private readonly ILogger<IGDataService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IGSessionService _igSessionManagementService;
        
        private readonly IReportParser<MemberDto> _memberReportParser;
        private readonly IReportParser<RoundDto> _roundReportParser;
        private readonly IReportParser<PlayerIdLookupDto> _playerIdLookupParser;
        private readonly IReportParser<ScorecardDto> _scorecardReportParser;
        private readonly IReportParser<TeeSheetDto> _teesheetReportParser;
        private readonly IReportParser<CompetitionDto> _competitionReportParser;
        private readonly IReportParser<MemberEventDto> _memberEventReportParser;
        private readonly IReportParser<CompetitionSettingsDto> _competitionSettingsReportParser;
        private readonly IReportParser<LeaderBoardDto> _leaderboardReportParser;
        private readonly IReportParser<SecurityLogEntryDto> _securityLogEntryParser;
        private readonly IReportParser<MemberCDHLookupDto> _memberCDHLookupReportParser;
        private readonly IReportParser<NewMemberResponseDto> _newMemberResponseReportParser;

        public IGDataService(IOptions<AppSettings> settings,
                                ILogger<IGDataService> logger,
                                IReportParser<MemberDto> memberReportParser,
                                IReportParser<RoundDto> roundReportParser,
                                IReportParser<PlayerIdLookupDto> playerIdLookupParser,
                                IReportParser<ScorecardDto> scorecardReportParser,
                                IReportParser<MemberEventDto> memberEventReportParser,
                                IReportParser<TeeSheetDto> teeSheetParser,
                                IReportParser<CompetitionDto> competitionReportParser,
                                IReportParser<CompetitionSettingsDto> competitionSettingsReportParser,
                                IReportParser<LeaderBoardDto> leaderBoardReportParser,
                                IReportParser<SecurityLogEntryDto> securityLogEntryParser,
                                IReportParser<MemberCDHLookupDto> memberCDHLookupReportParser,
                                IReportParser<NewMemberResponseDto> newMemberResponseReportParser,
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
            _memberEventReportParser = memberEventReportParser ?? throw new ArgumentNullException(nameof(memberEventReportParser));
            _teesheetReportParser = teeSheetParser ?? throw new ArgumentNullException(nameof(teeSheetParser));
            _competitionReportParser = competitionReportParser ?? throw new ArgumentNullException(nameof(competitionReportParser));
            _competitionSettingsReportParser = competitionSettingsReportParser ?? throw new ArgumentNullException(nameof(competitionSettingsReportParser));
            _leaderboardReportParser = leaderBoardReportParser ?? throw new ArgumentNullException(nameof(leaderBoardReportParser));
            _securityLogEntryParser = securityLogEntryParser ?? throw new ArgumentNullException(nameof(securityLogEntryParser));
            _memberCDHLookupReportParser = memberCDHLookupReportParser ?? throw new ArgumentNullException(nameof(memberCDHLookupReportParser));
            _newMemberResponseReportParser = newMemberResponseReportParser ?? throw new ArgumentNullException(nameof(newMemberResponseReportParser));
        }
        
        public async Task<List<MemberDto>> GetJuniorMembersAsync()
        {
            var playerIdLookup = await GetPlayerIdsByMemberAsync();
            var playerIdDictionary = playerIdLookup.ToDictionary(id => id.MemberId);

            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.JuniorMembershipReportUrl}";
            var members = await GetData<MemberDto>(reportUrl, _memberReportParser, __CACHE_JUNIORMEMBERS, TimeSpan.FromMinutes(_settings.Cache.ShortTerm_TTL_mins), HateOASLinks.GetMemberLinks);

            var now = DateTime.UtcNow;
            var cutoffDate = new DateTime(now.Year, 1, 1).AddYears(-18); // 1st January of the current year - 18 years

            var juniorMembers = members
                .Where(m => m.IsActive!.Value // Active members only
                    && (!m.LeaveDate.HasValue || m.LeaveDate!.Value >= now) // No past leave date
                    && m.DateOfBirth.HasValue && m.DateOfBirth.Value >= cutoffDate) // Was 18 or younger on 1st Jan
                .ToList();

            foreach (var m in juniorMembers)
            {
                m.PlayerId = playerIdDictionary.TryGetValue(m.MemberNumber!.Value, out PlayerIdLookupDto? player) ? player.PlayerId : null;
            }

            _logger.LogInformation("Filtered {Count} junior members from {Total} total members.", juniorMembers.Count, members.Count);

            return juniorMembers;
        }

        public async Task<List<MemberDto>> GetCurrentMembersAsync()
        {
            var playerIdLookup = await GetPlayerIdsByMemberAsync();
            var playerIdDictionary = playerIdLookup.ToDictionary(id => id.MemberId);

            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.AllCurrentMembersReportUrl}";
            var members = await GetData<MemberDto>(reportUrl, _memberReportParser, __CACHE_CURRENTMEMBERS, TimeSpan.FromMinutes(_settings.Cache.ShortTerm_TTL_mins), HateOASLinks.GetMemberLinks);

            var now = DateTime.UtcNow;

            var currentMembers = members
                .Where(m => m.IsActive!.Value // Active members only
                    && (!m.LeaveDate.HasValue || m.LeaveDate.Value >= now)) // No past leave date
                .ToList();

            foreach (var m in currentMembers)
            {
                m.PlayerId = playerIdDictionary.TryGetValue(m.MemberNumber!.Value, out PlayerIdLookupDto? player) ? player.PlayerId : null;
            }

            _logger.LogInformation("Filtered {Count} members from {Total} total members.", currentMembers.Count, members.Count);

            return currentMembers;
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
            
            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.MemberRoundsReportUrl.Replace("{playerId}", playerLookupId.PlayerId.ToString())}";
            var memberRounds = await GetData<RoundDto>(reportUrl, _roundReportParser, cacheKey, TimeSpan.FromMinutes(_settings.Cache.ShortTerm_TTL_mins), HateOASLinks.GetRoundLinks);

            _logger.LogInformation($"Retrieved {memberRounds.Count()} rounds for member {memberId}");

            return memberRounds;
        }

        public async Task<List<PlayerIdLookupDto>> GetPlayerIdsByMemberAsync()
        {
            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.PlayerIdLookupReportUrl}";
            var playerIdLookup = await GetData<PlayerIdLookupDto>(reportUrl, _playerIdLookupParser, __CACHE_PLAYERIDLOOKUP);

            _logger.LogInformation($"Retrieved {playerIdLookup.Count()} player lookup records.");

            return playerIdLookup;
        }

        public async Task<ScorecardDto?> GetScorecardForRoundAsync(string roundId)
        {
            var cacheKey = __CACHE_SCORECARDBYROUND.Replace("{roundId}", roundId);

            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.RoundReportUrl.Replace("{roundId}", roundId)}";
            var scorecards = await GetData<ScorecardDto>(reportUrl, _scorecardReportParser, cacheKey, TimeSpan.FromMinutes(_settings.Cache.LongTerm_TTL_mins));

            if (scorecards != null && scorecards.Any())
            {
                _logger.LogInformation($"Successfully retrieved the scorecard for round {roundId}.");

                return scorecards.FirstOrDefault();
            }

            return null;
        }

        public async Task<List<MemberEventDto>> GetMembershipEvents(DateTime fromDate, DateTime toDate)
        {
            var cacheKey = __CACHE_MEMBEREVENTHISTORYREPORT.Replace("{fromDate}", fromDate.ToString("yyyy-MM-dd")).Replace("{toDate}", toDate.ToString("yyyy-MM-dd"));

            var data = new Dictionary<string, string>
            {
                { "layout3", "1" },
                { "daterange", $"{fromDate.ToString("dd/MM/yyyy")} - {toDate.ToString("dd/MM/yyyy")}" },
                { "fromDate", fromDate.ToString("dd/MM/yyyy") },
                { "toDate", toDate.ToString("dd/MM/yyyy") }
            };

            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.MembershipEventHistoryReportUrl}";
            var events = await PostData<MemberEventDto>(reportUrl, data, _memberEventReportParser, cacheKey, TimeSpan.FromMinutes(_settings.Cache.MediumTerm_TTL_mins));

            _logger.LogInformation($"Successfully retrieved the {events.Count} member event records.");

            return events;
        }

        public async Task<List<MemberDto>> GetMembershipReportAsync()
        {
            var cacheKey = __CACHE_MEMBERREPORT;

            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.MembershipReportingUrl}";
            var members = await GetData<MemberDto>(reportUrl, _memberReportParser, cacheKey, TimeSpan.FromMinutes(_settings.Cache.LongTerm_TTL_mins));

            _logger.LogInformation($"Successfully retrieved the {members.Count} member records.");

            return members;
        }

        public async Task<TeeSheetDto?> GetTeeSheetByDateAsync(DateTime date)
        {
            var cacheKey = __CACHE_TEESHEET.Replace("{date}", date.ToString("yyyy-MM-dd"));
            var isToday = date.Date == DateTime.Today;

            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.TeeBookingsUrl}".Replace("{date}", date.ToString("dd-MM-yyyy"));
            var teesheet = await GetData<TeeSheetDto>(reportUrl, _teesheetReportParser, cacheKey, TimeSpan.FromMinutes(isToday ? _settings.Cache.ShortTerm_TTL_mins : _settings.Cache.Forever_TTL_Mins));

            if (teesheet != null && teesheet.Any())
            {
                _logger.LogInformation($"Successfully retrieved the teesheet for {date.ToString("dd MM yyyy")}.");

                return teesheet.FirstOrDefault();
            }

            return null;
        }

        public async Task<List<CompetitionDto>> GetActiveAndFutureCompetitionsAsync()
        {
            var activeCompetitionsUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.ActiveCompetitionsUrl}";
            var activeCompetitions = await GetData<CompetitionDto>(activeCompetitionsUrl, _competitionReportParser, __CACHE_ACTIVECOMPETITIONS, TimeSpan.FromMinutes(_settings.Cache.Default_TTL_Mins));

            var upcomingCompetitionsUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.UpcomingCompetitionsUrl}";
            var upcomingCompetitions = await GetData<CompetitionDto>(upcomingCompetitionsUrl, _competitionReportParser, __CACHE_FUTURECOMPETITIONS, TimeSpan.FromMinutes(_settings.Cache.ShortTerm_TTL_mins));


            if (activeCompetitions != null && upcomingCompetitions != null)
            {
                var result = activeCompetitions.Union(upcomingCompetitions).Where(c => c.Date >= DateTime.Today.Date).OrderBy(c => c.Date).ToList();

                _logger.LogInformation($"Successfully retrieved the {result.Count} current and future competitions.");

                return result;
            }

            return null;
        }

        public async Task<CompetitionSettingsDto?> GetCompetitionSettingsAsync(string competitionId)
        {
            var competitionSettingsUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.CompetitionSettingsUrl}".Replace("{compid}", competitionId);
            var competitionSettings = await GetData<CompetitionSettingsDto>(competitionSettingsUrl, _competitionSettingsReportParser, __CACHE_COMPETITIONSETTINGS, TimeSpan.FromMinutes(_settings.Cache.Default_TTL_Mins));

            if (competitionSettings != null && competitionSettings.Any())
            {
                _logger.LogInformation($"Successfully retrieved the competition settings or {competitionId}.");

                return competitionSettings.FirstOrDefault();
            }

            return null;
        }

        public async Task<LeaderBoardDto?> GetCompetitionLeaderboardAsync(string competitionId)
        {
            var leaderboardUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.LeaderBoardUrl}".Replace("{compid}", competitionId);
            var leaderboard = await GetData<LeaderBoardDto>(leaderboardUrl, _leaderboardReportParser, __CACHE_LEADERBOARD, TimeSpan.FromMinutes(_settings.Cache.Default_TTL_Mins));

            if (leaderboard != null && leaderboard.Any())
            {
                _logger.LogInformation($"Successfully retrieved the leaderboard for competition {competitionId}.");

                return leaderboard.FirstOrDefault();
            }

            return null;
        }

        public async Task<List<SecurityLogEntryDto>?> GetMobileOrders(DateTime? forDate = null)
        {
            forDate = forDate ?? DateTime.Now.Date;

            var securityLogUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.SecurityLogMobileOrders}".Replace("{today}", forDate?.ToString("dd-MM-yyyy"));
            var securityLog = await GetData<SecurityLogEntryDto>(securityLogUrl, _securityLogEntryParser);

            if (securityLog != null && securityLog.Any())
            {
                var deduplicated = new List<SecurityLogEntryDto>();

                SecurityLogEntryDto previous = null;
                foreach (var current in securityLog)
                {
                    if (previous == null || current.Event != previous.Event)
                    {
                        deduplicated.Add(current);
                    }
                    previous = current;
                }

                _logger.LogInformation($"Successfully retrieved {deduplicated.Count} mobile orders.");

                return deduplicated;
            }

            return null;
        }

        public async Task<MemberCDHLookupDto?> LookupMemberCDHIdDetails(string cdhId)
        {
            var url = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.MemberCDHLookupUrl}";
            var cacheKey = __CACHE_MEMBERCDHLOOKUP.Replace("{cdhid}", cdhId);

            var data = new Dictionary<string, string>(
            [
                new KeyValuePair<string, string>("cdh_id_lookup", cdhId)
            ]);

            var result = await this.PostData(url, data, _memberCDHLookupReportParser, cacheKey, TimeSpan.FromMinutes(_settings.Cache.ShortTerm_TTL_mins));

            if (result != null && result.Any())
            {
                return result.FirstOrDefault();
            }

            return null;
        }

        public async Task<NewMemberApplicationResultDto?> SubmitNewMemberApplicationAsync(NewMemberApplicationDto application)
        {
            var url = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.NewMembershipApplicationUrl}";
            
            MemberCDHLookupDto? cdhLookup = null;

            if (!string.IsNullOrEmpty(application.CdhId))
            {
                cdhLookup = await LookupMemberCDHIdDetails(application.CdhId);
            }

            var data = IGMembershipApplicationMapper.MapToFormData(application, cdhLookup);

            var result = await this.PostData(url, data, _newMemberResponseReportParser);

            if (result != null && result.Any())
            {
                return new NewMemberApplicationResultDto
                {
                    Application = application,
                    ApplicationId = application.ApplicationId,
                    MemberId = result[0].MemberId
                };
            }

            return null;
        }

        public async Task<bool> SetMemberProperty(MemberProperties property, int memberId, string value)
        {
            try
            {
                var url = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.NewMembershipApplicationUrl}".Replace("{memberid}", memberId.ToString());

                var content = new StringContent($"paramid={property.ToString()}&user_id={memberId}&param_value={value}");

                var data = new Dictionary<string, string>
                {
                    { "paramid", property.ToString() },
                    { "user_id", memberId.ToString() },
                    { "param_value", value }
                };

                var result = await this.PostData(url, data);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Failed to update property {property.GetDisplayName()} for member {memberId}", ex.Message);
                return false;
            }

            return true;
        }

        private async Task<string?> PostData(string reportUrl,
                                             Dictionary<string, string> data) 
        {
            // Step 1: Log in
            await _igSessionManagementService.WaitForLoginAsync();

            // Step 2: Fetch the report page
            _logger.LogInformation("Sending data to {Url}", reportUrl);
            var response = await _igSessionManagementService.PostPageContentRaw(reportUrl, data);

            return response;
        }

        private async Task<List<T>> PostData<T>(string reportUrl,
                                                Dictionary<string, string> data, 
                                                IReportParser<T> parser,
                                                string? cacheKey = null,
                                                TimeSpan? cacheTTL = null,
                                                Func<T, List<HateoasLink>>? linkBuilder = null) where T : HateoasResource, new()
        {
            ICacheService? cacheService = null;

            if (!string.IsNullOrEmpty(cacheKey))
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
            var doc = await _igSessionManagementService.PostPageContent(reportUrl, data);

            var items = parser.ParseReport(doc);

            // Step 4: Add Hateoas Links
            if (linkBuilder != null)
            {
                foreach (var item in items)
                {
                    item.Links = linkBuilder(item);
                }
            }

            if (!string.IsNullOrEmpty(cacheKey))
            {
                await cacheService.SetAsync(cacheKey!, items, cacheTTL!.Value).ConfigureAwait(false);
            }

            return items;
        }

        private async Task<List<T>> GetData<T>(string reportUrl,
                                               IReportParser<T> parser,
                                               string? cacheKey = null,
                                               TimeSpan? cacheTTL = null, 
                                               Func<T, List<HateoasLink>>? linkBuilder = null) where T : HateoasResource, new()
        {
            ICacheService? cacheService = null;

            if (!string.IsNullOrEmpty(cacheKey))
            {
                cacheTTL = cacheTTL ?? TimeSpan.FromMinutes(_settings.Cache.Default_TTL_Mins);

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

            if (!string.IsNullOrEmpty(cacheKey) && cacheTTL != null)
            {
                await cacheService.SetAsync(cacheKey!, items, cacheTTL!.Value).ConfigureAwait(false);
            }

            return items;
        }
    }
}
