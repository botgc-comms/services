using System.Net.Http.Headers;
using BOTGC.MemberPortal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace BOTGC.MemberPortal;

public static class ApiClientExtensions
{
    public static IServiceCollection AddApiClients(this IServiceCollection services, IConfiguration config)
    {
        services.AddHttpClient("Api", (sp, client) =>
        {
            var settings = sp.GetRequiredService<AppSettings>();
            client.BaseAddress = new Uri(settings.API.Url);
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            if (!string.IsNullOrWhiteSpace(settings.API.XApiKey))
            {
                client.DefaultRequestHeaders.Add("X-API-KEY", settings.API.XApiKey);
            }
            client.Timeout = TimeSpan.FromMinutes(5);
        })
        .SetHandlerLifetime(TimeSpan.FromMinutes(5))
        .AddPolicyHandler(GetRetryPolicy());

        return services;
    }

    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return Policy<HttpResponseMessage>
            .Handle<HttpRequestException>(ex => ex.InnerException is not TaskCanceledException)
            .OrResult(resp => (int)resp.StatusCode >= 500)
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))
            );
    }
}
