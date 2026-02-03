using System.Globalization;
using System.Text;
using System.Text.Json.Serialization;
using Azure.Storage.Queues;
using BOTGC.API;
using BOTGC.API.Common;
using BOTGC.API.Extensions;
using BOTGC.API.Interfaces;
using BOTGC.API.Services;
using BOTGC.API.Services.ReportServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using RedLockNet;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

var gb = CultureInfo.GetCultureInfo("en-GB");
CultureInfo.DefaultThreadCurrentCulture = gb;
CultureInfo.DefaultThreadCurrentUICulture = gb;

builder.Services
    .AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BOTGC Data API",
        Version = "v1",
        Description = "API for membership, competition and player data"
    });

    options.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API Key needed to access the Swagger UI. Enter your API key in the textbox below.",
        In = ParameterLocation.Header,
        Name = "X-API-KEY",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "ApiKeyScheme"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "ApiKey"
                }
            },
            Array.Empty<string>()
        }
    });

    options.OperationFilter<CacheControlHeaderFilter>();
    options.OperationFilter<AllowAnonymousOperationFilter>();
});

builder.Services.AddSingleton(sp =>
{
    var app = sp.GetRequiredService<IOptions<AppSettings>>().Value;

    if (string.IsNullOrWhiteSpace(app.Storage.ConnectionString))
    {
        throw new InvalidOperationException("AppSettings.Storage.ConnectionString is missing.");
    }

    return new QueueServiceClient(app.Storage.ConnectionString);
});

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
var appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>();
if (appSettings == null)
{
    throw new InvalidOperationException("AppSettings configuration is missing or invalid.");
}

builder.Services.AddSingleton(appSettings);

MembershipHelper.Configure(appSettings);

if (!builder.Environment.IsDevelopment())
{
    builder.Services.AddApplicationInsightsTelemetry(options =>
    {
        options.ConnectionString = appSettings.ApplicationInsights.ConnectionString;
    });

    builder.Logging.AddApplicationInsights(
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

builder.Services.AddSingleton<ITrophyFiles, TrophyFilesGitHub>();
builder.Services.AddSingleton<ITrophyDataStore, TrophyDataStore>();
builder.Services.AddSingleton<ITrophyService, TrophyService>();

builder.Services.AddSingleton<ICognitiveServices, AzureCognitiveServices>();
builder.Services.AddSingleton<IImageServices, ImageServices>();

builder.Services.AddSingleton<IMembershipReportingService, MembershipReportingService>();

builder.Services.AddHttpContextAccessor();

builder.Services.AddHealthChecks();

builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = null;
    serverOptions.Limits.KeepAliveTimeout = TimeSpan.FromMinutes(5);
    serverOptions.Limits.RequestHeadersTimeout = TimeSpan.FromMinutes(2);
});

var cacheServiceType = appSettings.Cache.Type;

if (string.Equals(cacheServiceType, "Redis", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = appSettings.Cache.RedisCache.ConnectionString;
        options.InstanceName = $"{appSettings.Cache.RedisCache.InstanceName}:";
    });

    builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
    {
        return ConnectionMultiplexer.Connect(appSettings.Cache.RedisCache.ConnectionString);
    });

    builder.Services.AddSingleton<RedLockFactory>(sp =>
    {
        var mux = sp.GetRequiredService<IConnectionMultiplexer>();

        return (RedLockFactory)RedLockFactory.Create(new List<RedLockMultiplexer>
        {
            new RedLockMultiplexer(mux),
        });
    });

    builder.Services.AddSingleton<IDistributedLockManager, RedLockDistributedLockManager>();

    builder.Services.AddSingleton<ICacheService, RedisCacheService>();
}
else if (string.Equals(cacheServiceType, "Memory", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddMemoryCache();
    builder.Services.AddScoped<ICacheService, MemoryCacheService>();
    builder.Services.AddSingleton<IDistributedLockManager, NoOpDistributedLockManager>();
}
else
{
    builder.Services.AddScoped<ICacheService, FileCacheService>();
    builder.Services.AddSingleton<IDistributedLockManager, NoOpDistributedLockManager>();
}

builder.Services.AddIGSupport();
builder.Services.RegisterEventDetectorsAndSubscribers();
builder.Services.AddQuizAttemptQueries(appSettings.QuizSettings.TableStorage.AttemptsTableName);
builder.Services.AddCourseAssessmentQueries(appSettings.CourseAssessmentSettings.TableStorage.CourseAssessmentTableName);

builder.Services.AddHttpClient();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var keyBytes = Encoding.UTF8.GetBytes(appSettings.Auth.App.JwtSigningKey);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = appSettings.Auth.App.JwtIssuer,

            ValidateAudience = true,
            ValidAudience = appSettings.Auth.App.JwtAudience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),

            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

var locOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(gb),
    SupportedCultures = new List<CultureInfo> { gb },
    SupportedUICultures = new List<CultureInfo> { gb }
};
app.UseRequestLocalization(locOptions);

app.MapGet("/", () => Results.Ok("Service is running"))
    .AllowAnonymous()
    .ExcludeFromDescription();

app.MapHealthChecks("/health")
    .ExcludeFromDescription();

app.MapGet("/version", () =>
{
    var sha = builder.Configuration["GIT_COMMIT_SHA"] ?? "unknown";
    return Results.Ok(sha);
})
.AllowAnonymous()
.ExcludeFromDescription();

app.UseWhen(context =>
    !context.Request.Path.StartsWithSegments("/swagger") &&
    !context.Request.Path.StartsWithSegments("/health") &&
    !context.Request.Path.StartsWithSegments("/version") &&
    !context.Request.Path.StartsWithSegments("/_diag") &&
    !context.Request.Path.StartsWithSegments("/api/auth/app/code") &&
    !context.Request.Path.StartsWithSegments("/api/auth/app/redeem") &&
    context.Request.Path != "/",
    appBuilder =>
    {
        appBuilder.UseMiddleware<AuthKeyMiddleware>();
    });

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
