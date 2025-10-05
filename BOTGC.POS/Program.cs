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
app.UseStaticFiles();
app.UseRouting();

app.MapHub<WastageHub>("/hubs/wastage");
app.MapHub<NgrokHub>("/hubs/ngrok");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Wastage}/{action=Index}/{id?}"
);

app.UseNgrokTunnel();

app.Run();
