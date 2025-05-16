namespace BOTGC.ManagementReports;

public class AppSettings
{
    public string AllowedCorsOrigins { get; set; } = string.Empty;
    public ApiSettings API { get; set; } = new();
    public GetAddressIOSettings GetAddressIOSettings { get; set; } = new();
    public GrowSurfSettings GrowSurfSettings { get; set; } = new();
}

public class ApiSettings
{
    public string Url { get; set; } = string.Empty;
    public string XApiKey { get; set; } = string.Empty;
}

public class GetAddressIOSettings
{
    public string ApiKey { get; set; } = string.Empty;
}

public class GrowSurfSettings
{
    public string CampaignId { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ParticipantAuthSecret { get; set; } = String.Empty;
}