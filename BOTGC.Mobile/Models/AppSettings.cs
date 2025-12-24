public sealed class ApiSettings
{
    public Api API { get; set; } = new Api();   
}

public sealed class  Api
{
    public string XApiKey { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
}