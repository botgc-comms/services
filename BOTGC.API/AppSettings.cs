namespace Services
{
    public class AppSettings
    {
        public string TrophyFilePath { get; set; }

        public AzureFaceApi AzureFaceApi { get; set; }

        public GitHub GitHub { get; set; }

        public Cache Cache { get; set; }

        public IG IG { get; set; }

    }

    public class Cache
    {
        public int ShortTerm_TTL_mins { get; set; } = 30;
        public int LongTerm_TTL_mins { get; set; } = 125000;

        public FileCacheStorage FileCacheStorage { get; set; }
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

        public required IGReports IGReports { get; set; } = new IGReports();

        public int LoginEveryNMinutes { get; set; } = 30;

    }

    public class IGReports
    {
        public string JuniorMembershipReportUrl { get; set; } = "/membership_reports.php?tab=report&section=viewreport&md=b52f6bd4cf74cc5dbfd84dec616ceb42";
        public string MemberRoundsReportUrl { get; set; } = "/roundmgmt.php?playerid={playerId}";
        public string PlayerIdLookupReportUrl { get; set; } = "/membership_reports.php?tab=status";
        public string RoundReportUrl { get; set; } = "/viewround.php?roundid={roundId}";
    }

    public class FileCacheStorage
    {
        public string Path { get; set; }
    }
}
