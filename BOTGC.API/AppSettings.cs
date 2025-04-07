namespace Services
{
    public class AppSettings
    {
        public string TrophyFilePath { get; set; }

        public AzureFaceApi AzureFaceApi { get; set; }

        public GitHub GitHub { get; set; }

        public Cache Cache { get; set; }

        public IG IG { get; set; }

        public string PlayingMemberExpression { get; set; } = "^(?:5|6|7|Intermediate).*?$";
        public string NonPlayingMemberExpression { get; set; } = "^(?!5|6|7|Intermediate|1894|Corporate|Staff|Professional).+$";

    }

    public class Cache
    {
        public string Type { get; set; } = "File";
        public int Default_TTL_Mins { get; set; } = 12 * 60;
        public int ShortTerm_TTL_mins { get; set; } = 30;
        public int MediumTerm_TTL_mins { get; set; } = 12*60;
        public int LongTerm_TTL_mins { get; set; } = 24*60*30;
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
        public string RepoUrl { get; set; } = string.Empty; // e.g., "https://github.com/user/repo"
        public string ApiUrl { get; set; } = string.Empty; // e.g., "https://api.github.com/repos/user/repo"
        public string RawUrl { get; set; } = string.Empty; // e.g., "https://raw.githubusercontent.com/user/repo/main"
        public string TrophyDirectory { get; set; } = "trophies"; // Directory inside repo
    }

    public class IG
    {
        public string BaseUrl { get; set; } = "https://www.botgc.co.uk";
        public required string MemberId { get; set; } 
        public required string MemberPassword { get; set; } 
        public required string AdminPassword { get; set; }

        public required IGEndpoints Urls { get; set; } = new IGEndpoints();

        public int LoginEveryNMinutes { get; set; } = 30;

    }

    public class IGEndpoints
    {
        public string JuniorMembershipReportUrl { get; set; } = "/membership_reports.php?tab=report&section=viewreport&md=b52f6bd4cf74cc5dbfd84dec616ceb42";
        public string AllCurrentMembersReportUrl { get; set; } = "/membership_reports.php?tab=report&section=viewreport&md=5d71e7119d780dba4850506f622c1cfb";
        public string MemberRoundsReportUrl { get; set; } = "/roundmgmt.php?playerid={playerId}";
        public string PlayerIdLookupReportUrl { get; set; } = "/membership_reports.php?tab=status";
        public string RoundReportUrl { get; set; } = "/viewround.php?roundid={roundId}";
        public string MembershipReportingUrl { get; set; } = "/membership_reports.php?tab=report&section=viewreport&md=9be9f71c8988351887840f3826a552da";
        public string MembershipEventHistoryReportUrl { get; set; } = "/membership_reports.php?tab=categorychanges&requestType=ajax&ajaxaction=getreport";
        public string NewMembershipApplicationUrl { get; set; } = "/membership_addmember.php?&requestType=ajax&ajaxaction=confirmadd";
        public string TeeBookingsUrl { get; set; } = "/teetimes.php?date={date}";
        public string UpcomingCompetitionsUrl { get; set; } = "/compdash.php?tab=competitions&requestType=ajax&ajaxaction=morecomps&status=upcoming&entrants=all&kind=all&teamsolo=all&year=all&offset=0&limit=20";
        public string ActiveCompetitionsUrl { get; set; } = "/compdash.php?tab=competitions&requestType=ajax&ajaxaction=morecomps&status=active&entrants=all&kind=all&teamsolo=all&year=all&offset=0&limit=20";
        public string CompetitionSettingsUrl { get; set; } = "/compadmin3.php?compid={compid}&tab=settings";
        public string LeaderBoardUrl { get; set; } = "/competition.php?compid={compid}&preview=1";
    }

    public class FileCacheStorage
    {
        public string Path { get; set; }
    }

    public class RedisCacheStorage
    {
        public string ConnectionString { get; set; }
    }
}
