using System.Text.Json;
using System.Text.Json.Serialization;
using BOTGC.MemberPortal;
using BOTGC.MemberPortal.Extensions;
using BOTGC.MemberPortal.Hubs;
using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Services;
using BOTGC.MemberPortal.Services.TileAdapters;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using RedLockNet.SERedis;
using RedLockNet.SERedis.Configuration;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);

builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                       | ForwardedHeaders.XForwardedProto
                       | ForwardedHeaders.XForwardedHost;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

builder.Services.AddSingleton(new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
});

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();

builder.Services.AddSingleton<NgrokState>();

builder.Services.AddPosApiClients(builder.Configuration);

builder.Services.AddSignalR();

builder.Services.AddScoped<IUserAuthenticationService, InMemoryUserAuthenticationService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddScoped<IVoucherService, ApiVoucherService>();

builder.Services.AddScoped<ITileService, TileService>();
builder.Services.AddScoped<ITileAdapter, CadetVouchersTileAdapter>();
builder.Services.AddScoped<ITileAdapter, JuniorMentorTileAdapter>();
builder.Services.AddScoped<ITileAdapter, RulesQuizTileAdapter>();
builder.Services.AddScoped<ITileAdapter, HandicapSessionTileAdapter>();
builder.Services.AddScoped<ITileAdapter, CategoryProgressTileAdapter>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

builder.Services.AddScoped<IJuniorMemberDirectoryService, JuniorMemberDirectoryService>();
builder.Services.AddScoped<ICheckRideReportRepository, TableStorageCheckRideReportRepository>();

var settings = builder.Configuration.GetSection("AppSettings").Get<AppSettings>();

builder.Services.AddStackExchangeRedisCache(options =>
{
    if (string.IsNullOrWhiteSpace(settings?.Cache?.ConnectionString))
    {
        throw new InvalidOperationException("AppSettings:Cache:ConnectionString is required.");
    }

    options.Configuration = settings.Cache.ConnectionString;

    if (!string.IsNullOrWhiteSpace(settings.Cache.InstanceName))
    {
        options.InstanceName = $"{settings.Cache.InstanceName}:";
    }
});

builder.Services.AddScoped<ICacheService, RedisCacheService>();

var redisConnection = ConnectionMultiplexer.Connect(settings?.Cache?.ConnectionString);
var redLockFactory = RedLockFactory.Create(new List<RedLockMultiplexer> { redisConnection });

builder.Services.AddSingleton(redLockFactory);
builder.Services.AddSingleton<IDistributedLockManager, RedLockDistributedLockManager>();


builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromHours(8);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
});


builder.Services.AddJuniorQuizContentFromFileSystem();

builder.Services.AddLearningPacksCommon();
builder.Services.AddLearningPacksFromFileSystem();

var app = builder.Build();

app.UseForwardedHeaders();

app.UseStaticFiles();
app.UseRouting();
app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NgrokHub>("/hubs/ngrok");
app.MapHub<VoucherHub>("/hubs/voucherHub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.UseNgrokTunnel();

app.Run();
