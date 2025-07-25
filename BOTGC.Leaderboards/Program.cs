using BOTGC.Leaderboards;
using BOTGC.Leaderboards.Interfaces;
using BOTGC.Leaderboards.Services;

var builder = WebApplication.CreateBuilder(args);

var environment = builder.Environment;

var mvcBuilder = builder.Services.AddControllersWithViews();

if (environment.IsDevelopment())
{
    mvcBuilder.AddRazorRuntimeCompilation();
}
builder.Services.AddSignalR();

builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
builder.Services.AddSingleton<IConfiguration>(builder.Configuration.GetSection("AppSettings"));

builder.Services.AddHttpClient<IJuniorEclecticService, JuniorEclecticService>();
builder.Services.AddHttpClient<ILeaderboardService, LeaderboardService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.Use(async (context, next) =>
{
    var nonce = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    context.Items["CSPNonce"] = nonce;
    await next();
});

app.UseStaticFiles();
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllerRoute(
        name: "default",
        pattern: "{controller=JuniorEclectic}/{action=Index}");
});

app.MapHub<CompetitionHub>("/competitionhub");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
