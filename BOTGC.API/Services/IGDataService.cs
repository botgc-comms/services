using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Extensions;
using System.Collections.Specialized;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        private const string __CACHE_COMPETITIONSETTINGS = "Competition_Settings_{compid}";
        private const string __CACHE_COMPETITIONSUMMARY = "Competition_Summary_{compid}";
        private const string __CACHE_LEADERBOARD = "Leaderboard_Settings_{compid}";
        private const string __CACHE_MEMBERCDHLOOKUP = "MemberCDHLookup_{cdhid}";
        private const string __CACHE_MEMBERSHIPCATEGORIES = "Member_Categories";

        private readonly AppSettings _settings;
        private readonly ILogger<IGDataService> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IGSessionService _igSessionManagementService;
        private readonly ITaskBoardService _taskBoardService;


        private readonly IReportParser<MemberDto> _memberReportParser;
        private readonly IReportParser<RoundDto> _roundReportParser;
        private readonly IReportParser<PlayerIdLookupDto> _playerIdLookupParser;
        private readonly IReportParser<ScorecardDto> _scorecardReportParser;
        private readonly IReportParser<TeeSheetDto> _teesheetReportParser;
        private readonly IReportParser<CompetitionDto> _competitionReportParser;
        private readonly IReportParser<MemberEventDto> _memberEventReportParser;
        private readonly IReportParser<CompetitionSettingsDto> _competitionSettingsReportParser;
        private readonly IReportParser<CompetitionSummaryDto> _competitionSummaryReportParser;
        private readonly IReportParser<SecurityLogEntryDto> _securityLogEntryParser;
        private readonly IReportParser<MemberCDHLookupDto> _memberCDHLookupReportParser;
        private readonly IReportParser<NewMemberResponseDto> _newMemberResponseReportParser;

        private readonly IReportParserWithMetadata<LeaderBoardDto, CompetitionSettingsDto> _leaderboardReportParser;
        private readonly IReportParserWithMetadata<ChampionshipLeaderboardPlayerDto, CompetitionSettingsDto> _clubChampionshipLeaderboardReportParser;

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
                                IReportParser<CompetitionSummaryDto> competitionSummaryReportParser,
                                IReportParser<SecurityLogEntryDto> securityLogEntryParser,
                                IReportParser<MemberCDHLookupDto> memberCDHLookupReportParser,
                                IReportParser<NewMemberResponseDto> newMemberResponseReportParser,
                                IReportParserWithMetadata<LeaderBoardDto, CompetitionSettingsDto> leaderBoardReportParser,
                                IReportParserWithMetadata<ChampionshipLeaderboardPlayerDto, CompetitionSettingsDto> clubChampionshipLeaderboardReportParser,
                                ITaskBoardService taskBoardService,
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
            _competitionSummaryReportParser = competitionSummaryReportParser ?? throw new ArgumentNullException(nameof(competitionSummaryReportParser));
            _leaderboardReportParser = leaderBoardReportParser ?? throw new ArgumentNullException(nameof(leaderBoardReportParser));
            _clubChampionshipLeaderboardReportParser = clubChampionshipLeaderboardReportParser ?? throw new ArgumentNullException(nameof(clubChampionshipLeaderboardReportParser));
            _securityLogEntryParser = securityLogEntryParser ?? throw new ArgumentNullException(nameof(securityLogEntryParser));
            _memberCDHLookupReportParser = memberCDHLookupReportParser ?? throw new ArgumentNullException(nameof(memberCDHLookupReportParser));
            _newMemberResponseReportParser = newMemberResponseReportParser ?? throw new ArgumentNullException(nameof(newMemberResponseReportParser));

            _taskBoardService = taskBoardService ?? throw new ArgumentNullException(nameof(taskBoardService));
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

            // Combine both lists, handling nulls
            var allCompetitions = new List<CompetitionDto>();
            if (activeCompetitions != null) allCompetitions.AddRange(activeCompetitions);
            if (upcomingCompetitions != null) allCompetitions.AddRange(upcomingCompetitions);

            // Remove duplicates by Id
            allCompetitions = allCompetitions
                .Where(c => c.Id.HasValue)
                .GroupBy(c => c.Id.Value)
                .Select(g => g.First())
                .ToList();

            // Fix missing dates by fetching settings
            foreach (var comp in allCompetitions.Where(c => !c.Date.HasValue))
            {
                if (comp.Id.HasValue && comp.Id.Value > 0)
                {
                    var settings = await GetCompetitionSettingsAsync(comp.Id.Value.ToString());
                    if (settings != null)
                    {
                        comp.Date = settings.Date;
                        comp.MultiPartCompetition = settings.MultiPartCompetition;
                    }
                    else
                    {
                        _logger.LogWarning($"No settings found for competition {comp.Id}.");
                    }
                }
            }

            // Only return competitions with a valid date in the future
            var result = allCompetitions
                .Where(c => c.Date.HasValue && c.Date.Value >= DateTime.Today.Date)
                .OrderBy(c => c.Date)
                .ToList();

            _logger.LogInformation($"Successfully retrieved {result.Count} current and future competitions.");

            return result;
        }

        public async Task<CompetitionSettingsDto?> GetCompetitionSettingsAsync(string competitionId)
        {
            var competitionSettingsUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.CompetitionSettingsUrl}".Replace("{compid}", competitionId);
            var competitionSettings = await GetData<CompetitionSettingsDto>(competitionSettingsUrl, _competitionSettingsReportParser, __CACHE_COMPETITIONSETTINGS.Replace("{compid}", competitionId), TimeSpan.FromMinutes(_settings.Cache.Default_TTL_Mins));

            var competitionSummaryUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.CompetitionSummaryUrl}".Replace("{compid}", competitionId);
            var competitionSummary = await GetData<CompetitionSummaryDto>(competitionSummaryUrl, _competitionSummaryReportParser, __CACHE_COMPETITIONSUMMARY.Replace("{compid}", competitionId), TimeSpan.FromMinutes(_settings.Cache.Default_TTL_Mins));

            if (competitionSettings != null && competitionSettings.Any())
            {
                _logger.LogInformation($"Successfully retrieved the competition settings or {competitionId}.");

                var retVal = competitionSettings.FirstOrDefault();
                if (retVal != null)
                {
                    retVal.MultiPartCompetition = competitionSummary.FirstOrDefault()?.MultiPartCompetition;
                }

                return retVal;
            }

            return null;
        }

        public async Task<LeaderBoardDto?> GetCompetitionLeaderboardAsync(string competitionId)
        {
            var competitionSettings = await this.GetCompetitionSettingsAsync(competitionId);

            var grossOrNett = competitionSettings.ResultsDisplay.ToLower().Contains("net") ? "1" : "2";
            var leaderboardUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.LeaderBoardUrl}".Replace("{compid}", competitionId).Replace("{grossOrNett}", grossOrNett);
            var leaderboard = await GetData<LeaderBoardDto, CompetitionSettingsDto>(leaderboardUrl, _leaderboardReportParser, competitionSettings, __CACHE_LEADERBOARD.Replace("{compid}", competitionId), TimeSpan.FromMinutes(_settings.Cache.VeryShortTerm_TTL_mins));

            if (leaderboard != null && leaderboard.Any())
            {
                _logger.LogInformation($"Successfully retrieved the leaderboard for competition {competitionId}.");
                
                var retVal = leaderboard.FirstOrDefault();
                retVal.CompetitionDetails = competitionSettings;

                return retVal;
            }

            return null;
        }

        public async Task<ClubChampionshipLeaderBoardDto?> GetClubChampionshipsLeaderboardAsync(string competitionId)
        {
            var competitionSettings = await this.GetCompetitionSettingsAsync(competitionId);

            if (competitionSettings.MultiPartCompetition != null && competitionSettings.MultiPartCompetition.Count == 2)
            {
                var grossOrNett = "2"; // Club champs is a gross competition

                var r1Id = competitionSettings.MultiPartCompetition.FirstOrDefault(
                    r => Regex.IsMatch(r.Key, "R(?:ound)?\\s*1", RegexOptions.IgnoreCase),
                    competitionSettings.MultiPartCompetition.ElementAt(0));

                var r1Url = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.LeaderBoardUrl}"
                    .Replace("{compid}", r1Id.Value.ToString()).Replace("{grossOrNett}", grossOrNett);

                var r1 = await GetData<ChampionshipLeaderboardPlayerDto, CompetitionSettingsDto>(
                    r1Url, _clubChampionshipLeaderboardReportParser, competitionSettings,
                    __CACHE_LEADERBOARD.Replace("{compid}", competitionId) + "_R1",
                    TimeSpan.FromMinutes(_settings.Cache.VeryShortTerm_TTL_mins));

                var r2Id = competitionSettings.MultiPartCompetition.FirstOrDefault(
                    r => Regex.IsMatch(r.Key, "R(?:ound)?\\s*2", RegexOptions.IgnoreCase),
                    competitionSettings.MultiPartCompetition.ElementAt(1));

                var r2Url = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.LeaderBoardUrl}"
                    .Replace("{compid}", r2Id.Value.ToString()).Replace("{grossOrNett}", grossOrNett);

                var r2 = await GetData<ChampionshipLeaderboardPlayerDto, CompetitionSettingsDto>(
                    r2Url, _clubChampionshipLeaderboardReportParser, competitionSettings,
                    __CACHE_LEADERBOARD.Replace("{compid}", competitionId) + "_R2",
                    TimeSpan.FromMinutes(_settings.Cache.VeryShortTerm_TTL_mins));

                var combined = new List<ChampionshipLeaderboardPlayerDto>();

                if (r1 != null && r1.Any())
                {
                    var allPlayers = r1.Select(p => p.PlayerName)
                        .Union(r2?.Select(p => p.PlayerName) ?? Enumerable.Empty<string>())
                        .Distinct();

                    foreach (var playerName in allPlayers)
                    {
                        var p1 = r1.FirstOrDefault(x => x.PlayerName == playerName);
                        var p2 = r2?.FirstOrDefault(x => x.PlayerName == playerName);

                        var toPar1 = ParseToPar(p1?.Par);
                        var toPar2 = ParseToPar(p2?.Par);
                        var totalPar = toPar1 + toPar2;
                        var totalParStr = totalPar == 0 ? "LEVEL" : (totalPar > 0 ? $"+{totalPar}" : totalPar.ToString());

                        var thru1 = ParseThru(p1?.Thru);
                        var thru2 = ParseThru(p2?.Thru);
                        var combinedThru = (thru1 + thru2).ToString();

                        var combinedPlayer = new ChampionshipLeaderboardPlayerDto
                        {
                            PlayerName = playerName,
                            PlayerId = p1?.PlayerId ?? p2?.PlayerId,
                            Par = totalParStr,
                            R1 = p1?.Score,
                            R2 = p2?.Score,
                            Countback = p2?.Countback ?? p1?.Countback,
                            Thru = combinedThru,
                            Score = ((TryParseInt(p1?.Score) + TryParseInt(p2?.Score)).ToString()),
                            Position = null
                        };

                        combined.Add(combinedPlayer);
                    }

                    combined = combined
                        .OrderBy(x => ParseToPar(x.Par))
                        .ThenBy(x => TryParseThru(x.Thru))
                        .ThenBy(x => int.TryParse(x.R2, out var r2Score) ? r2Score : (int.TryParse(x.R1, out var r1Score) ? r1Score : int.MaxValue))
                        .ThenBy(x => ParseCountback(x.Countback).Back9)
                        .ThenBy(x => ParseCountback(x.Countback).Back6)
                        .ThenBy(x => ParseCountback(x.Countback).Back3)
                        .ThenBy(x => ParseCountback(x.Countback).Back1)
                        .ToList();

                    for (int i = 0; i < combined.Count; i++)
                    {
                        combined[i].Position = i + 1;
                    }

                    return new ClubChampionshipLeaderBoardDto
                    {
                        CompetitionDetails = competitionSettings,
                        Round1 = r1,
                        Round2 = r2,
                        Total = combined
                    };
                }

                int ParseToPar(string par)
                {
                    if (string.IsNullOrWhiteSpace(par)) return 0;
                    par = par.Trim().ToUpper();
                    if (par == "LEVEL") return 0;
                    if (par.StartsWith("+")) return int.TryParse(par.Substring(1), out var n) ? n : 0;
                    if (par.StartsWith("-")) return int.TryParse(par, out var n) ? n : 0;
                    return int.TryParse(par, out var x) ? x : 0;
                }

                int ParseThru(string thru)
                {
                    if (string.IsNullOrWhiteSpace(thru)) return 0;
                    if (int.TryParse(thru, out var n)) return n;
                    return 0;
                }

                int TryParseInt(string? val)
                {
                    if (int.TryParse(val, out var n)) return n;
                    return 0;
                }

                int TryParseThru(string? thru)
                {
                    if (int.TryParse(thru, out var holes)) return holes;
                    return 18;
                }

                (int Back9, int Back6, int Back3, int Back1) ParseCountback(string countback)
                {
                    var regex = new Regex(@"Back 9 - (?<b9>[\d.]+), Back 6 - (?<b6>[\d.]+), Back 3 - (?<b3>[\d.]+), Back 1 - (?<b1>[\d.]+)");
                    var match = regex.Match(countback ?? "");
                    if (!match.Success)
                        return (int.MaxValue, int.MaxValue, int.MaxValue, int.MaxValue);
                    return (
                        (int)double.Parse(match.Groups["b9"].Value),
                        (int)double.Parse(match.Groups["b6"].Value),
                        (int)double.Parse(match.Groups["b3"].Value),
                        (int)double.Parse(match.Groups["b1"].Value)
                    );
                }
            }

            return new ClubChampionshipLeaderBoardDto
            {
                CompetitionDetails = competitionSettings
            };
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

        public async Task<List<MembershipCategoryGroupDto>> GetMembershipCategories()
        {
            ICacheService? cacheService = null;

            var cacheKey = __CACHE_MEMBERSHIPCATEGORIES;

            using var scope = _serviceScopeFactory.CreateScope();
            cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var cachedResults = await cacheService!.GetAsync<List<MembershipCategoryGroupDto>>(cacheKey).ConfigureAwait(false);
            if (cachedResults != null && cachedResults.Any())
            {
                _logger.LogInformation("Retrieving results from cache for Membership Categories...");
                return cachedResults;
            }

            var result = await this._taskBoardService.GetMembershipCategories();

            await cacheService.SetAsync(cacheKey!, result, TimeSpan.FromMinutes(_settings.Cache.ShortTerm_TTL_mins)).ConfigureAwait(false);

            return result;
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
                var url = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.UpdateMemberPropertiesUrl}".Replace("{memberid}", memberId.ToString());

                var content = new StringContent($"paramid=1&user_id={memberId}&param_value={value}");

                var data = new Dictionary<string, string>
                {
                    { "paramid", ((int)property).ToString() },
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

            if (doc != null)
            {

                var items = await parser.ParseReport(doc);

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
            else
            {
                _logger.LogError("Failed to retrieve data from {Url} for {ReportType}", reportUrl, typeof(T).Name);

                return null;
            }
        }

        private async Task<List<T>> GetData<T>(
            string reportUrl,
            IReportParser<T> parser,
            string? cacheKey = null,
            TimeSpan? cacheTTL = null,
            Func<T, List<HateoasLink>>? linkBuilder = null
        ) where T : HateoasResource, new()
        {
            return await ExecuteGet(reportUrl, async doc => await parser.ParseReport(doc), cacheKey, cacheTTL, linkBuilder);
        }

        private async Task<List<T>> GetData<T, TMetadata>(
            string reportUrl,
            IReportParserWithMetadata<T, TMetadata> parser,
            TMetadata metadata,
            string? cacheKey = null,
            TimeSpan? cacheTTL = null,
            Func<T, List<HateoasLink>>? linkBuilder = null
        ) where T : HateoasResource, new()
        {
            return await ExecuteGet(reportUrl, async doc => await parser.ParseReport(doc, metadata), cacheKey, cacheTTL, linkBuilder);
        }

        private async Task<List<T>> ExecuteGet<T>(
            string reportUrl,
            Func<HtmlDocument, Task<List<T>>> parse,
            string? cacheKey,
            TimeSpan? cacheTTL,
            Func<T, List<HateoasLink>>? linkBuilder
        ) where T : HateoasResource, new()
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

            var items = await parse(doc);

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
