using BOTGC.MembershipApplication;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

var environment = builder.Environment;

var mvcBuilder = builder.Services.AddControllersWithViews();

if (environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}

// Bind AppSettings
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddSingleton(sp =>
    sp.GetRequiredService<IOptions<AppSettings>>().Value);

// Setup HTTP client with retry policy
builder.Services.AddHttpClient("MembershipApi", (sp, client) =>
{
    var settings = sp.GetRequiredService<AppSettings>();
    client.BaseAddress = new Uri(settings.API.Url);
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

// CSP Middleware
app.Use(async (context, next) =>
{
    var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();
    var settings = context.RequestServices.GetRequiredService<AppSettings>();
    var apiUrl = settings.API.Url;

    var scriptSrc = new List<string>
    {
        "'self'",
        "https://cdn.jsdelivr.net",
        "https://www.google.com",
        "https://www.gstatic.com",
        "https://cdn.getaddress.io"
    };

    var connectSrc = new List<string>
    {
        "'self'",
        apiUrl,
        "https://www.google.com",
        "https://www.gstatic.com",
        "https://api.getaddress.io"
    };

    if (env.IsDevelopment())
    {
        scriptSrc.Add("'unsafe-inline'");
        connectSrc.Add("http://localhost:*");
        connectSrc.Add("https://localhost:*");
        connectSrc.Add("ws://localhost:*");
        connectSrc.Add("wss://localhost:*");
    }

    var frameSrc = new List<string>
    {
        "'self'",
        "https://www.google.com",
        "https://localhost:5001"
    };

    var csp = string.Join(" ", new[]
    {
        "default-src 'self';",
        $"script-src {string.Join(" ", scriptSrc)};",
        "style-src 'self' 'unsafe-inline';",
        "img-src 'self' data:;",
        "font-src 'self' data:;",
        $"connect-src {string.Join(" ", connectSrc)};",
        $"frame-src {string.Join(" ", frameSrc)}",
        $"form-action 'self' {apiUrl};",
        "base-uri 'self';",
        "object-src 'none';"
    });

    context.Response.Headers["Content-Security-Policy"] = csp;

    await next();
});

app.UseCors("AllowedOriginsPolicy");
app.UseStaticFiles();
app.UseRouting();

app.MapDefaultControllerRoute();

app.Run();
