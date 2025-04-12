namespace BOTGC.Leaderboards;

public class AppSettings
{
    public API API { get; set; } = new API();
}

public class API
{
    public string XApiKey { get; set; } = "";
    public string Url { get; set; } = "";
}

