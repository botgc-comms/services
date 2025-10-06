using BOTGC.POS;
using BOTGC.POS.Hubs;
using BOTGC.POS.Services;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);

builder.Services.AddSingleton<IOperatorService, HttpOperatorService>();
builder.Services.AddSingleton<IProductService, HttpProductService>();
builder.Services.AddSingleton<IReasonService, InMemoryReasonService>();
builder.Services.AddSingleton<IWasteService, HttpWasteService>();
builder.Services.AddSingleton<NgrokState>();

builder.Services.AddPosApiClients(builder.Configuration);

var signalR = builder.Services.AddSignalR();
var redisConn = builder.Configuration["AppSettings:Redis:ConnectionString"];
if (!string.IsNullOrWhiteSpace(redisConn))
{
    signalR.AddStackExchangeRedis(redisConn, o => { o.Configuration.ChannelPrefix = "pos-wastage"; });
}

builder.Services.AddHttpContextAccessor();

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

var gatekeeper = new GatekeeperOptions
{
    QueryKey = "k",
    SharedSecret = builder.Configuration["AppSettings:Access:SharedSecret"] ?? "",
    CookieName = builder.Configuration["AppSettings:Access:CookieName"] ?? "pos_access",
    RedirectUrl = "/access?returnUrl={returnUrl}",
    // AllowedCidrs = new[] { "10.0.0.0/8", "192.168.0.0/16" } // optional
};
app.UseMiddleware<GatekeeperMiddleware>(gatekeeper);

app.UseStaticFiles();
app.UseRouting();

app.MapHub<WastageHub>("/hubs/wastage");
app.MapHub<NgrokHub>("/hubs/ngrok");

app.MapGet("/access", (HttpContext ctx, IConfiguration cfg) =>
{
    var secret = cfg["AppSettings:Access:SharedSecret"] ?? "";
    var cookieName = cfg["AppSettings:Access:CookieName"] ?? "pos_access";
    var ttlDaysStr = cfg["AppSettings:Access:CookieTtlDays"];
    var ttlDays = int.TryParse(ttlDaysStr, out var d) ? d : 30;

    var provided = ctx.Request.Query["k"].ToString();
    var returnUrl = ctx.Request.Query["returnUrl"].ToString();

    if (!string.IsNullOrEmpty(provided) && provided == secret)
    {
        ctx.Response.Cookies.Append(cookieName, secret, new CookieOptions
        {
            HttpOnly = true,
            Secure = ctx.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            Expires = DateTimeOffset.UtcNow.AddDays(ttlDays)
        });

        return Results.Redirect(string.IsNullOrEmpty(returnUrl) ? "/" : returnUrl);
    }

    return Results.Content("Not authorised. Append ?k=<key> (use your QR) to gain access.");
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Wastage}/{action=Index}/{id?}"
);

app.UseNgrokTunnel();

app.Run();
