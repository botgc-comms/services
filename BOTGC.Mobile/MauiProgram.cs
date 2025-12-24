using BOTGC.Mobile.Interfaces;
using BOTGC.Mobile.Pages;
using BOTGC.Mobile.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp.Views.Maui.Controls.Hosting;
using ZXing.Net.Maui.Controls;

namespace BOTGC.Mobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

#if DEBUG
        var env = "Development";
#else
var env = "Production";
#endif

        AddJsonFromMauiAsset(builder.Configuration, "appsettings.json", optional: false);
        AddJsonFromMauiAsset(builder.Configuration, $"appsettings.{env}.json", optional: true);

        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .UseBarcodeReader()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

#if DEBUG
        builder.Logging.AddDebug();
#endif
        builder.Services.Configure<AppSettings>(builder.Configuration.GetSection("AppSettings"));
        builder.Services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);

        builder.Services.AddHttpClient("BotgcApi", (sp, client) =>
        {
            var settings = sp.GetRequiredService<AppSettings>();

            if (!Uri.TryCreate(settings.Api.Url, UriKind.Absolute, out var baseUri))
            {
                throw new InvalidOperationException("API:Url is not a valid absolute URI.");
            }

            client.BaseAddress = baseUri;
            client.Timeout = TimeSpan.FromSeconds(30);

            if (!string.IsNullOrWhiteSpace(settings.Api.XApiKey))
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("X-API-KEY", settings.Api.XApiKey);
            }

            client.DefaultRequestHeaders.TryAddWithoutValidation("X-CLIENT-ID", settings.Api.ClientId); 
        });

        builder.Services.AddTransient<PairingInfoPage>();
        builder.Services.AddTransient<AppAuthPage>();
        builder.Services.AddTransient<DateOfBirthPage>();
        builder.Services.AddTransient<WebShellPage>();
        builder.Services.AddTransient<MetallicSplashPage>();

        builder.Services.AddSingleton<AppNavigationCoordinator>();
        builder.Services.AddSingleton<DateOfBirthFlowHandler>();

        builder.Services.AddSingleton<IAppAuthService, AppAuthService>();
        builder.Services.AddSingleton<IDeepLinkService, DeepLinkService>();
        builder.Services.AddSingleton<INavigationGate, NavigationGate>();

        return builder.Build();
    }

    private static void AddJsonFromMauiAsset(ConfigurationManager config, string assetName, bool optional)
    {
        try
        {
            using var stream = FileSystem.OpenAppPackageFileAsync(assetName).GetAwaiter().GetResult();
            config.AddJsonStream(stream);
        }
        catch
        {
            if (!optional)
            {
                throw;
            }
        }
    }
}
