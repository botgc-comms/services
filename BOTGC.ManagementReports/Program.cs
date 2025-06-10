using BOTGC.ManagementReports;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient(); // Enables API calls

// Bind AppSettings
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);

// Setup HTTP client with retry policy
builder.Services.AddHttpClient("MembershipApi", (sp, client) =>
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
.AddPolicyHandler(
    Policy<HttpResponseMessage>
        .Handle<HttpRequestException>(ex => ex.InnerException is not TaskCanceledException)
        .OrResult(response => (int)response.StatusCode >= 500)
        .WaitAndRetryAsync(
            3,
            retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
            (outcome, timespan, retryAttempt, context) =>
            {
                Console.WriteLine($"Retry {retryAttempt} after {timespan.TotalSeconds}s due to: " +
                    (outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()));
                return Task.CompletedTask;
            }
        )
);

var app = builder.Build();

app.UseStaticFiles();
app.UseRouting();
app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=MembershipReport}/{action=Index}/{id?}");
});

app.Run();
