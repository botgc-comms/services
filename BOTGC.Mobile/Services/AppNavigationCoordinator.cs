// Services/AppNavigationCoordinator.cs
using BOTGC.Mobile.Interfaces;
using BOTGC.Mobile.Pages;

namespace BOTGC.Mobile.Services;

public sealed class AppNavigationCoordinator
{
    private readonly IAppAuthService _authService;
    private readonly DeepLinkService _deepLinkService;
    private readonly IServiceProvider _services;
    private readonly INavigationGate _navGate;

    private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

    public AppNavigationCoordinator(
        IAppAuthService authService,
        IDeepLinkService deepLinkService,
        IServiceProvider services,
        INavigationGate navGate)
    {
        _authService = authService;
        _deepLinkService = (DeepLinkService)deepLinkService;
        _services = services;
        _navGate = navGate;

        _deepLinkService.LinkReceived += DeepLinkService_LinkReceived;

        _ = DrainPendingAsync();
    }

    private async Task DrainPendingAsync()
    {
        while (_deepLinkService.TryDequeue(out var uri) && uri != null)
        {
            await HandleIncomingLinkAsync(uri);
        }
    }

    private async void DeepLinkService_LinkReceived(object? sender, Uri uri)
    {
        await HandleIncomingLinkAsync(uri);
    }

    public async Task HandleIncomingLinkAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        if (uri == null)
        {
            return;
        }

        _navGate.Take();

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var authResult = await _authService.CheckAndRefreshIfPossibleAsync(cancellationToken);
            if (authResult.IsAuthenticated)
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    var services = Application.Current!.Handler!.MauiContext!.Services;
                    Application.Current!.MainPage = new NavigationPage(services.GetRequiredService<WebShellPage>());
                });

                return;
            }

            var code = _authService.ExtractCodeFromRedeemUrl(uri.ToString());
            if (string.IsNullOrWhiteSpace(code))
            {
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    await EnsureMainNavRootAsync(_services.GetRequiredService<PairingInfoPage>());
                });

                return;
            }

            var redeem = await _authService.RedeemAsync(code, cancellationToken);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var dobPage = _services.GetRequiredService<DateOfBirthPage>();
                dobPage.SetSessionId(redeem.SessionId);

                await EnsureMainNavRootAsync(dobPage);
            });
        }
        finally
        {
            _gate.Release();
        }
    }

    private static async Task EnsureMainNavRootAsync(Page page)
    {
        if (Application.Current!.MainPage is NavigationPage nav)
        {
            var root = nav.Navigation.NavigationStack.FirstOrDefault();
            if (root != null)
            {
                nav.Navigation.InsertPageBefore(page, root);
                await nav.PopToRootAsync(false);
                return;
            }
        }

        Application.Current!.MainPage = new NavigationPage(page);
    }
}
