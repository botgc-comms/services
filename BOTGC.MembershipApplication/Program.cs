using BOTGC.MembershipApplication;
using BOTGC.MembershipApplication.Services;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http.Headers;

var builder = WebApplication.CreateBuilder(args);

var environment = builder.Environment;

var mvcBuilder = builder.Services.AddControllersWithViews();

if (environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}

// Bind AppSettings
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);

// Configure Dependency Injection
builder.Services.AddHttpClient<IReferralService, GrowSurfService>();
builder.Services.AddHttpContextAccessor();

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
