using BOTGC.MemberPortal;
using BOTGC.MemberPortal.Hubs;
using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Services;
using BOTGC.MemberPortal.Services.TileAdapters;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);

builder.Services.AddSingleton<NgrokState>();

builder.Services.AddPosApiClients(builder.Configuration);

var signalR = builder.Services.AddSignalR();
var redisConn = builder.Configuration["AppSettings:Redis:ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConn))
{
    signalR.AddStackExchangeRedis(redisConn, o => { o.Configuration.ChannelPrefix = "member-portal"; });
}

builder.Services.AddHttpContextAccessor();

// Auth + current user
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
    });

builder.Services.AddAuthorization();

builder.Services.AddScoped<IUserAuthenticationService, InMemoryUserAuthenticationService>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

builder.Services.AddScoped<ITileService, TileService>();

builder.Services.AddScoped<ITileAdapter, CadetVouchersTileAdapter>();
builder.Services.AddScoped<ITileAdapter, JuniorMentorTileAdapter>();
builder.Services.AddScoped<ITileAdapter, RulesQuizTileAdapter>();
builder.Services.AddScoped<ITileAdapter, HandicapSessionTileAdapter>();
builder.Services.AddScoped<ITileAdapter, CategoryProgressTileAdapter>();
builder.Services.AddScoped<IVoucherService, InMemoryVoucherService>();


builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                       | ForwardedHeaders.XForwardedProto
                       | ForwardedHeaders.XForwardedHost;
    o.KnownNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

var appSettings = app.Services.GetRequiredService<AppSettings>();
var accessSettings = appSettings.Access ?? new Access();

//var gatekeeper = new GatekeeperOptions
//{
//    QueryKey = "k",
//    SharedSecret = accessSettings.SharedSecret ?? string.Empty,
//    CookieName = accessSettings.CookieName ?? "pos_access",
//    RedirectUrl = "/access?returnUrl={returnUrl}"
//};

//app.UseMiddleware<GatekeeperMiddleware>(gatekeeper);

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapHub<NgrokHub>("/hubs/ngrok");
app.MapHub<VoucherHub>("/hubs/voucherHub");

//app.MapGet("/access", (HttpContext ctx, AppSettings settings) =>
//{
//    var access = settings.Access;
//    if (access is null || string.IsNullOrEmpty(access.SharedSecret))
//    {
//        return Results.Content("Access not configured.");
//    }

//    var secret = access.SharedSecret;
//    var cookieName = access.CookieName ?? "pos_access";
//    var ttlDays = access.CookieTtlDays <= 0 ? 365 : access.CookieTtlDays;

//    var provided = ctx.Request.Query["k"].ToString();
//    var returnUrl = ctx.Request.Query["returnUrl"].ToString();

//    if (!string.IsNullOrEmpty(provided) && provided == secret)
//    {
//        ctx.Response.Cookies.Append(cookieName, secret, new CookieOptions
//        {
//            HttpOnly = true,
//            Secure = ctx.Request.IsHttps,
//            SameSite = SameSiteMode.Lax,
//            Path = "/",
//            Expires = DateTimeOffset.UtcNow.AddDays(ttlDays)
//        });

//        return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
//    }

//    return Results.Content("Not authorised. Append ?k=<key> (use your QR) to gain access.");
//});

//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Wastage}/{action=Index}/{id?}"
//);

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.UseNgrokTunnel();

app.Run();
