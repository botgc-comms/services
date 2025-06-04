using BOTGC.MembershipApplication;
using BOTGC.MembershipApplication.Interfaces;
using BOTGC.MembershipApplication.Services;
using BOTGC.MembershipApplication.Services.Background;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http.Headers;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var environment = builder.Environment;

Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Console.OutputEncoding = Encoding.UTF8;

var mvcBuilder = builder.Services.AddControllersWithViews();

if (environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}

// Bind AppSettings
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);

var appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>();

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = appSettings.ApplicationInsights.ConnectionString;
    });

    builder.Logging.AddApplicationInsights (
         configureTelemetryConfiguration: (config) =>
         {
             config.ConnectionString = appSettings.ApplicationInsights.ConnectionString;
         },
         configureApplicationInsightsLoggerOptions: options =>
         {
             options.IncludeScopes = true;
             options.TrackExceptionsAsExceptionTelemetry = true;
         }
    );

    builder.Logging.AddFilter<Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider>(
        "", LogLevel.Information);
}

// Configure Dependency Injection
builder.Services.AddHttpClient<IReferralService, GrowSurfService>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<IMembershipCategoryCache, MembershipCategoryCache>();
builder.Services.AddHostedService<MembershipCategoryPollingService>();

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
})
.SetHandlerLifetime(TimeSpan.FromMinutes(5))
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))));

// Configure CORS
var corsOrigins = builder.Configuration["AppSettings:AllowedCorsOrigins"]
    ?.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowedOriginsPolicy", policy =>
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (!environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHttpsRedirection();
    app.UseHsts();
}

app.UseContentSecurityPolicy()
   .WithGrowSurf()
   .ExcludePaths("/embed-test/index.html")
   .Build();

app.UseCors("AllowedOriginsPolicy");
app.UseStaticFiles();
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "/",
        defaults: new { controller = "Membership", action = "Apply" });

    endpoints.MapControllerRoute(
        name: "fallback",
        pattern: "{controller=Membership}/{action=Apply}/{id?}");
});

app.MapDefaultControllerRoute();


app.Run();
