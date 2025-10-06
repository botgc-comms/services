namespace BOTGC.POS;

public class AppSettings
{
    public string AllowedCorsOrigins { get; set; } = string.Empty;
    public ApiSettings API { get; set; } = new();
    public Access? Access { get; set; }
}

public class ApiSettings
{
    public string Url { get; set; } = string.Empty;
    public string XApiKey { get; set; } = string.Empty;
}

public class Access
{
    public string? SharedSecret { get; set; }
    public string? CookieName { get; set; }
    public int CookieTtlDays { get; set; } = 30;
}