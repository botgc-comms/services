using BOTGC.Mobile.Interfaces;
using Microsoft.Maui.Hosting;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Windows.ApplicationModel.Activation;

namespace BOTGC.Mobile.WinUI;

public partial class App : MauiWinUIApplication
{
    private static bool _wired;

    public App()
    {
        InitializeComponent();

        if (_wired)
        {
            return;
        }

        _wired = true;

        var keyInstance = AppInstance.FindOrRegisterForKey("main");

        if (!keyInstance.IsCurrent)
        {
            var args = AppInstance.GetCurrent().GetActivatedEventArgs();
            _ = keyInstance.RedirectActivationToAsync(args);

            try
            {
                Microsoft.UI.Xaml.Application.Current.Exit();
            }
            finally
            {
                Environment.Exit(0);
            }
        }

        AppInstance.GetCurrent().Activated += (_, e) =>
        {
            if (e.Kind == ExtendedActivationKind.Protocol &&
                e.Data is IProtocolActivatedEventArgs p &&
                p.Uri != null)
            {
                PublishDeepLink(p.Uri);
            }
        };

        var firstArgs = AppInstance.GetCurrent().GetActivatedEventArgs();
        if (firstArgs.Kind == ExtendedActivationKind.Protocol &&
            firstArgs.Data is IProtocolActivatedEventArgs firstProto &&
            firstProto.Uri != null)
        {
            PublishDeepLink(firstProto.Uri);
        }
    }

    protected override MauiApp CreateMauiApp()
    {
        return MauiProgram.CreateMauiApp();
    }

    private static void PublishDeepLink(Uri uri)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var services = Microsoft.Maui.Controls.Application.Current?.Handler?.MauiContext?.Services;
            var deepLinkService = services?.GetService(typeof(IDeepLinkService)) as IDeepLinkService;
            deepLinkService?.Publish(uri);
        });
    }
}
