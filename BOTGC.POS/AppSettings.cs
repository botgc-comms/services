namespace BOTGC.POS;

public class AppSettings
{
    public string AllowedCorsOrigins { get; set; } = string.Empty;
    public ApiSettings API { get; set; } = new();
    public Access? Access { get; set; }
    public StockTakeUiSettings StockTake { get; set; } = new();
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

public sealed class StockTakeUiSettings
{
    public bool ShowEstimatedInDialog { get; init; } = false;
    public bool EnforceSameSelectedOperatorAccrossDevices { get; init; } = false;
    public string StockTakeUrl { get; init; } = "./StockTake";
    public bool ShowDailyStockAlert { get; set; } = false;

    public bool EnableStockTakeAlert { get; set; } = false;
    public bool EnableStockTakeForFood { get; set; } = false;
    public bool EnableChime { get; init; } = false;
    public string ChimeStartTime { get; init; } = "16:30";
    public string ChimeEndTime { get; init; } = "23:00";
    public int ChimeIntervalMinutes { get; init; } = 15;
}

