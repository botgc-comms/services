using BOTGC.API;
using BOTGC.API.Common;
using BOTGC.API.Extensions;
using BOTGC.API.Interfaces;
using BOTGC.API.Services;
using BOTGC.API.Services.ReportServices;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
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
});

// Setup application configuration
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddSingleton<IConfiguration>(builder.Configuration.GetSection("AppSettings"));

var appSettings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>();
MembershipHelper.Configure(appSettings);

//builder.Services.AddSingleton<TrophyFilesDiskStorage>();
builder.Services.AddSingleton<ITrophyFiles, TrophyFilesGitHub>();
builder.Services.AddSingleton<ITrophyDataStore, TrophyDataStore>();
builder.Services.AddSingleton<ITrophyService, TrophyService>();

builder.Services.AddSingleton<ICognitiveServices, AzureCognitiveServices>();
builder.Services.AddSingleton<IImageServices, ImageServices>();

builder.Services.AddSingleton<IMembershipReportingService, MembershipReportingService>();

builder.Services.AddHttpContextAccessor();

var cacheServiceType = appSettings.Cache.Type;

if (string.Equals(cacheServiceType, "Redis", StringComparison.OrdinalIgnoreCase))
{
    // Register Redis distributed cache provider so IDistributedCache is available.
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = appSettings.Cache.RedisCache.ConnectionString;
        options.InstanceName = $"{appSettings.Cache.RedisCache.InstanceName}:";
    });

    builder.Services.AddScoped<ICacheService, RedisCacheService>();
}
else if (string.Equals(cacheServiceType, "Memory", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddMemoryCache();
    builder.Services.AddScoped<ICacheService, MemoryCacheService>();
}
else
{
    builder.Services.AddScoped<ICacheService, FileCacheService>();
}

// Add support for interacting with IG
builder.Services.AddIGSupport();

var app = builder.Build();

app.UseWhen(context => !context.Request.Path.StartsWithSegments("/swagger"), appBuilder =>
{
    appBuilder.UseMiddleware<AuthKeyMiddleware>();
});

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
