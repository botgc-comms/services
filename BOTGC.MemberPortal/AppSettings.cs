using BOTGC.MemberPortal.Models;
using System.Reflection.Metadata;

namespace BOTGC.MemberPortal;

public class AppSettings
{
    public string AllowedCorsOrigins { get; set; } = string.Empty;
    public ApiSettings API { get; set; } = new();
    public Access? Access { get; set; }
    public List<TileDefinition> Tiles { get; set; } = new List<TileDefinition>();
    public Cache Cache { get; set; } = new();
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
    public int CookieTtlDays { get; set; } = 365;
}

public class Cache
{
    public string ConnectionString { get; set; }
    public string InstanceName { get; set; } = "BOTGC.API";
}

