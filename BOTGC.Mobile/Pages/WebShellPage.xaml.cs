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

    public WebShellPage(IAppAuthService authService, AppSettings settings)
    {
        _authService = authService;
        _settings = settings;

        InitializeComponent();

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

    private static string NormaliseBaseUrl(string baseUrl)
    {
        baseUrl = (baseUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException("AppSettings:Web:BaseUrl is required.");
        }

        return baseUrl.TrimEnd('/');
    }
}
