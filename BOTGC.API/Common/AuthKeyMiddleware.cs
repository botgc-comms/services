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
        var apiKey = GetApiKey(context);

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Authorisation key was not provided.");
            return;
        }

        if (!string.Equals(apiKey, _settings.Auth.XApiKey, StringComparison.Ordinal))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsync("Unauthorized.");
            return;
        }

        await _next(context);
    }

    private static string? GetApiKey(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-API-KEY", out var headerValue))
        {
            var value = headerValue.ToString();
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        if (context.Request.Query.TryGetValue("x-api-key", out var queryValue))
        {
            var value = queryValue.ToString();
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        return null;
    }
}
