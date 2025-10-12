﻿namespace BOTGC.API
{
    public class AppSettings
    {
        public AuthSettings Auth { get; set; } = new();

        public bool UseCachedHtml { get; set; } = false;

        public string TrophyFilePath { get; set; }

        public int ConcurrentRequestThrottle { get; set; } = 5;

        public AzureFaceApi AzureFaceApi { get; set; } = new();

        public GitHub GitHub { get; set; } = new();

        public Cache Cache { get; set; } = new();

        public IG IG { get; set; } = new();

        public QueueSettings Queue { get; set; } = new();

        public MondaySettings Monday { get; set; } = new();

        public FeatureToggles FeatureToggles { get; set; } = new();

        public Waste Waste { get; set; } = new();

        public StockTakeSettings StockTake { get; set; } = new();

        public ApplicationInsightsSettings ApplicationInsights { get; set; } = new();

        public string PlayingMemberExpression { get; set; } = "^(?:5|6|7|Intermediate|MX).*?$";
        public string NonPlayingMemberExpression { get; set; } = "^(?!5|6|7|Intermediate|MX|1894|Corporate|Staff|Professional|Test).+$";

    }

    public class ApplicationInsightsSettings
    {
        public string ConnectionString { get; set; } = string.Empty;
    }

    public class MondaySettings
    {
        public string APIKey { get; set; }
    }

    public class QueueSettings
    {
        public string ConnectionString { get; set; }
        public string Name { get; set; }
    }
    public class AuthSettings
    {
        public string XApiKey { get; set; } = "";
    }

    public class Cache
    {
        public string Type { get; set; } = "File";
        public int Default_TTL_Mins { get; set; } = 12 * 60;
        public int VeryShortTerm_TTL_mins { get; set; } = 1;
        public int ShortTerm_TTL_mins { get; set; } = 30;
        public int MediumTerm_TTL_mins { get; set; } = 12 * 60;
        public int LongTerm_TTL_mins { get; set; } = 24 * 60 * 30;
        public int Forever_TTL_Mins { get; set; } = int.MaxValue;

        public FileCacheStorage FileCacheStorage { get; set; }
        public RedisCacheStorage RedisCache { get; set; }
    }

    public class AzureFaceApi
    {
        public string EndPoint { get; set; } = string.Empty;
        public string SubscriptionKey { get; set; } = string.Empty;
    }

    public class GitHub
    {
        public string Token { get; set; } = "";
        public string RepoUrl { get; set; } = "https://github.com/botgc-comms/data";
        public string ApiUrl { get; set; } = "https://api.github.com";
        public string RawUrl { get; set; } = "https://raw.githubusercontent.com/botgc-comms/data/master";
        public string TrophyDirectory { get; set; } = "Trophies";
    }

    public class IG
    {
        public string BaseUrl { get; set; } = "https://www.botgc.co.uk";
        public string MemberId { get; set; }
        public string MemberPassword { get; set; }
        public string AdminPassword { get; set; }

        public IGEndpoints Urls { get; set; } = new IGEndpoints();

        public int LoginEveryNMinutes { get; set; } = 30;

    }

    public class IGEndpoints
    {
        public string JuniorMembershipReportUrl { get; set; } = "/membership_reports.php?tab=report&section=viewreport&md=b52f6bd4cf74cc5dbfd84dec616ceb42";
        public string LadyMembersReportUrl { get; set; } = "/membership_reports.php?tab=report&section=viewreport&md=5d71e7119d780dba4850506f622c1cfb";
        public string AllCurrentMembersReportUrl { get; set; } = "/membership_reports.php?tab=report&section=viewreport&md=5d71e7119d780dba4850506f622c1cfb";
        public string NewMemberLookupReportUrl { get; set; } = "/membership_reports.php?tab=newmembers";
        public string AllWaitingMembersReportUrl { get; set; } = "/membership_reports.php?tab=report&section=viewreport&md=6da7bd30935f3f5f2374aa8206cd80ec";
        public string MemberRoundsReportUrl { get; set; } = "/roundmgmt.php?playerid={playerId}";
        public string HandicapIndexHistoryReportUrl { get; set; } = "/roundmgmt.php?playerid={playerId}";
        public string PlayerIdLookupReportUrl { get; set; } = "/membership_reports.php?tab=status";
        public string RoundReportUrl { get; set; } = "/viewround.php?roundid={roundId}";
        public string MembershipReportingUrl { get; set; } = "/membership_reports.php?tab=report&section=viewreport&md=9be9f71c8988351887840f3826a552da";
        public string MembershipEventHistoryReportUrl { get; set; } = "/membership_reports.php?tab=categorychanges&requestType=ajax&ajaxaction=getreport";
        public string NewMembershipApplicationUrl { get; set; } = "/membership_addmember.php?&requestType=ajax&ajaxaction=confirmadd";
        public string MemberCDHLookupUrl { get; set; } = "/membership_addmember.php?&requestType=ajax&ajaxaction=cdhidlookup";
        public string TeeBookingsUrl { get; set; } = "/teetimes.php?date={date}";
        public string UpcomingCompetitionsUrl { get; set; } = "/compdash.php?tab=competitions&requestType=ajax&ajaxaction=morecomps&status=upcoming&entrants=all&kind=all&teamsolo=all&year=all&offset=0&limit=20";
        public string ActiveCompetitionsUrl { get; set; } = "/compdash.php?tab=competitions&requestType=ajax&ajaxaction=morecomps&status=active&entrants=all&kind=all&teamsolo=all&year=all&offset=0&limit=20";
        public string CompetitionSettingsUrl { get; set; } = "/compadmin3.php?compid={compid}&tab=settings";
        public string CompetitionSummaryUrl { get; set; } = "/compadmin3.php?compid={compid}&tab=summary";
        public string LeaderBoardUrl { get; set; } = "/competition.php?compid={compid}&preview=1&sort={grossOrNett}";
        public string SecurityLogMobileOrders { get; set; } = "/log.php?search=Mobile+order&person=&start={today}&starttime=&end={today}&endtime=";
        public string UpdateMemberPropertiesUrl { get; set; } = "/member.php?memberid={memberid}&requestType=ajax&ajaxaction=saveparamvalue";
        public string ConfirmAddWastageUrl { get; set; } = "/tillstockcontrol.php?&requestType=ajax&ajaxaction=confirmaddwastage ";
        public string GetStockTakesReportUrl { get; set; } = "/tillstockcontrol.php?tab=transactions&requestType=ajax&ajaxaction=updatedata";
        public string MemberDetailsUrl { get; set; } = "/member.php?memberid={memberid}";
        public string StockItemsUrl { get; set; } = "/tillstockcontrol.php";
        public string TillOperatorsReportUrl { get; set; } = "/tilladmin.php?tab=operators";
    }

    public class FileCacheStorage
    {
        public string Path { get; set; }
    }

    public class RedisCacheStorage
    {
        public string ConnectionString { get; set; }
        public string InstanceName { get; set; } = "BOTGC.API";
    }

    public class FeatureToggles
    {
        public bool EnableStockControlSchedule { get; set; } = true;
        public bool ProcessMembershipApplications { get; set; } = true;
    }

    public class Waste
    {
        public int DaysToLookBack { get; set; } = 7;
        public int DefaultStockRoom { get; set; } = 3;
        public string Cron { get; set; } = "0 3 * * *";
        public string TimeZone { get; set; } = "Europe/London";
        public bool RunOnStartup { get; set; } = false;
    }

    public class StockTakeSettings
    {
        public Dictionary<string, int> StockTakeComplexity { get; set; } = new(StringComparer.OrdinalIgnoreCase);
        public string Cron { get; init; } = "0 3 * * *"; // 03:00 every day
        public string TimeZone { get; init; } = "Europe/London";
        public bool RunOnStartup { get; init; } = true;
        public int DaysToLookBack { get; init; } = 10;   // clamp inside flusher
        public List<string> Divisions { get; init; } = new(); // e.g. ["WINES","MINERALS","SNACKS","BEER CANS","DRAUGHT BEER"]
    }
}
