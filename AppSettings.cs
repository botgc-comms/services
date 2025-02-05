namespace Services
{
    public class AppSettings
    {
        public string TrophyFilePath { get; set; }

        public AzureFaceApi AzureFaceApi { get; set; }

        public GitHub GitHub { get; set; }

        public Cache Cache { get; set; }
    }

    public class Cache
    {
        public int TTL_mins { get; set; } = 30;
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
}
