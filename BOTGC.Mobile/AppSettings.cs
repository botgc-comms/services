using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BOTGC.Mobile;

public sealed class AppSettings
{
    public ApiEndpointSettings Api { get; set; } = new();
    public WebSettings Web { get; set; } = new();
}

public sealed class ApiEndpointSettings
{
    public string XApiKey { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ClientId { get; set; } = string.Empty;    
}

public sealed class WebSettings
{
    public string BaseUrl { get; set; } = string.Empty;
}
