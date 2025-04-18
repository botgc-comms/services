using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;


namespace BOTGC.API.Common;

public class AuthKeyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly AppSettings _settings;

    public AuthKeyMiddleware(IOptions<AppSettings> settings, RequestDelegate next)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _next = next;
    }

    public async Task Invoke(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue("X-API-KEY", out var extractedApiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Authorisation key was not provided.");
            return;
        }
        if (!string.Equals(extractedApiKey, _settings.Auth.XApiKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Unauthorized.");
            return;
        }

        await _next(context);
    }
}
