using System.Collections.Generic;
using BOTGC.Mobile.Interfaces;
using Microsoft.Maui.Controls;

namespace BOTGC.Mobile;

public partial class WebShellPage : ContentPage
{
    private const string SsoPath = "/app/sso";

    private readonly IAppAuthService _authService;
    private readonly AppSettings _settings;

    private readonly Dictionary<string, string> _routes;

    private bool _ssoAttempted;
    private bool _ssoEstablished;

    private bool _firstNavigationCompleted;

    public WebShellPage(IAppAuthService authService, AppSettings settings)
    {
        _authService = authService;
        _settings = settings;

        InitializeComponent();

        Browser.Navigating += Browser_Navigating;
        Browser.Navigated += Browser_Navigated;

        var baseUrl = NormaliseBaseUrl(_settings.Web.BaseUrl);

        _routes = new Dictionary<string, string>
        {
            ["home"] = $"{baseUrl}/",
            ["progress"] = $"{baseUrl}/progress",
            ["vouchers"] = $"{baseUrl}/vouchers",
            ["mentor"] = $"{baseUrl}/mentor",
            ["play"] = $"{baseUrl}/play"
        };

        BottomNav.TabTapped += Navigate;

        LoadingOverlay.IsVisible = true;
        LoadingOverlay.Opacity = 1;

        _ = NavigateWithSsoAsync("home");
    }

    private void Navigate(string key)
    {
        _ = NavigateWithSsoAsync(key);
    }

    private async Task NavigateWithSsoAsync(string key)
    {
        if (!_routes.TryGetValue(key, out var url))
        {
            return;
        }

        if (_ssoEstablished)
        {
            Browser.Source = new UrlWebViewSource { Url = url };
            return;
        }

        if (!_ssoAttempted)
        {
            _ssoAttempted = true;

            var sso = await _authService.IssueWebSsoCodeAsync();
            if (sso != null && !string.IsNullOrWhiteSpace(sso.Value.Code))
            {
                _ssoEstablished = true;

                var baseUrl = NormaliseBaseUrl(_settings.Web.BaseUrl);
                var returnUrl = Uri.EscapeDataString(url);
                var ssoUrl = $"{baseUrl}{SsoPath}?code={Uri.EscapeDataString(sso.Value.Code)}&returnUrl={returnUrl}";

                Browser.Source = new UrlWebViewSource { Url = ssoUrl };
                return;
            }
        }

        Browser.Source = new UrlWebViewSource { Url = url };
    }

    private void Browser_Navigating(object? sender, WebNavigatingEventArgs e)
    {
        if (_firstNavigationCompleted)
        {
            return;
        }

        LoadingOverlay.IsVisible = true;
        LoadingOverlay.Opacity = 1;
    }

    private static string NormaliseBaseUrl(string baseUrl)
    {
        baseUrl = (baseUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("AppSettings:Web:BaseUrl is required.");
        }

        return baseUrl.TrimEnd('/');
    }

    private async void Browser_Navigated(object? sender, WebNavigatedEventArgs e)
    {
        UpdateSelectedTabFromUrl(e.Url);

        _firstNavigationCompleted = true;

        await LoadingOverlay.FadeTo(0, 120);
        LoadingOverlay.IsVisible = false;
        LoadingOverlay.Opacity = 1;
    }

    private void UpdateSelectedTabFromUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return;
        }

        var key = MapPathToNavKey(uri.AbsolutePath);
        if (key == null)
        {
            return;
        }

        if (!string.Equals(BottomNav.SelectedKey, key, StringComparison.OrdinalIgnoreCase))
        {
            BottomNav.SelectedKey = key;
        }
    }

    private static string? MapPathToNavKey(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "home";
        }

        path = path.Trim();

        if (path.Length > 1 && path.EndsWith("/", StringComparison.Ordinal))
        {
            path = path[..^1];
        }

        if (path.Equals("", StringComparison.Ordinal) || path.Equals("/", StringComparison.Ordinal))
        {
            return "home";
        }

        if (path.Equals("/progress", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/progress/", StringComparison.OrdinalIgnoreCase))
        {
            return "progress";
        }

        if (path.Equals("/vouchers", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/vouchers/", StringComparison.OrdinalIgnoreCase))
        {
            return "vouchers";
        }

        if (path.Equals("/mentor", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/mentor/", StringComparison.OrdinalIgnoreCase))
        {
            return "mentor";
        }

        if (path.Equals("/play", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/play/", StringComparison.OrdinalIgnoreCase))
        {
            return "play";
        }

        if (path.Equals("/home", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/home/", StringComparison.OrdinalIgnoreCase))
        {
            return "home";
        }

        return null;
    }

}
