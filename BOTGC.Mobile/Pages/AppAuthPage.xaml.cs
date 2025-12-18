using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using Microsoft.Maui.ApplicationModel;

#if ANDROID || IOS || MACCATALYST
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
#endif

namespace BOTGC.Mobile.Pages;

public partial class AppAuthPage : ContentPage
{
    private const string ApiBaseUrl = "https://botgc.link/";
    private const string ApiKeyHeaderName = "X-API-KEY";
    private const string ClientIdHeaderName = "X-CLIENT-ID";
    private const string StorageRefreshToken = "botgc.refresh_token";

    private readonly HttpClient _httpClient = new HttpClient
    {
        BaseAddress = new Uri(ApiBaseUrl),
        Timeout = TimeSpan.FromSeconds(30),
    };

    private string _apiKey = string.Empty;
    private string _clientId = "botgc-maui";

    private string? _pendingSessionId;

#if ANDROID || IOS || MACCATALYST
    private CameraBarcodeReaderView? _cameraView;
    private bool _scannerInitialised;
#endif

    public AppAuthPage()
    {
        InitializeComponent();
        DobPicker.Date = DateTime.Today.AddYears(-18);

        _apiKey = Preferences.Get("botgc.api_key", string.Empty);
        _clientId = Preferences.Get("botgc.client_id", "botgc-maui");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await AttemptAutoSignInAsync();
    }

    protected override void OnDisappearing()
    {
#if ANDROID || IOS || MACCATALYST
        if (_cameraView != null)
        {
            _cameraView.IsDetecting = false;
        }
#endif
        base.OnDisappearing();
    }

    private async Task AttemptAutoSignInAsync()
    {
        StatusLabel.Text = "Checking sign-in…";
        DobPanel.IsVisible = false;
        ManualPanel.IsVisible = false;

        var refreshToken = await SecureStorage.GetAsync(StorageRefreshToken);
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            await ShowScannerAsync();
            return;
        }

        try
        {
            var tokens = await RefreshAsync(refreshToken);
            await SaveTokensAsync(tokens);

            StatusLabel.Text = "Signed in.";
            await Navigation.PushAsync(new WebShellPage());
        }
        catch
        {
            SecureStorage.Remove(StorageRefreshToken);
            await ShowScannerAsync();
        }
    }

    private async Task ShowScannerAsync()
    {
        _pendingSessionId = null;
        DobPanel.IsVisible = false;

        ScannerHelpLabel.Text = "Use the camera to scan your QR code.";
        ManualPanel.IsVisible = false;

#if ANDROID || IOS || MACCATALYST
        if (!ZXing.Net.Maui.BarcodeScanning.IsSupported)
        {
            ScannerHelpLabel.Text = "Camera scanning is not available here. Paste the redeem URL instead.";
            ManualPanel.IsVisible = true;
            return;
        }

        var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
        if (status != PermissionStatus.Granted)
        {
            status = await Permissions.RequestAsync<Permissions.Camera>();
        }

        if (status != PermissionStatus.Granted)
        {
            ScannerHelpLabel.Text = "Camera permission was not granted. Paste the redeem URL instead.";
            ManualPanel.IsVisible = true;
            return;
        }

        EnsureScannerView();
        if (_cameraView != null)
        {
            _cameraView.IsDetecting = true;
        }

        StatusLabel.Text = "Point the camera at the QR code.";
        return;
#else
        ScannerHelpLabel.Text = "Camera scanning is not available on this platform. Paste the redeem URL instead.";
        ManualPanel.IsVisible = true;
        StatusLabel.Text = "Paste the redeem URL to continue.";
        return;
#endif
    }

    private void ShowDob(string sessionId)
    {
        _pendingSessionId = sessionId;
        StatusLabel.Text = "Enter your date of birth.";
        DobPanel.IsVisible = true;
        ManualPanel.IsVisible = false;

#if ANDROID || IOS || MACCATALYST
        if (_cameraView != null)
        {
            _cameraView.IsDetecting = false;
        }
#endif
    }

#if ANDROID || IOS || MACCATALYST
    private void EnsureScannerView()
    {
        if (_scannerInitialised)
        {
            return;
        }

        _scannerInitialised = true;

        _cameraView = new CameraBarcodeReaderView
        {
            HorizontalOptions = LayoutOptions.Fill,
            VerticalOptions = LayoutOptions.Fill,
            IsDetecting = true,
            Options = new BarcodeReaderOptions
            {
                Formats = BarcodeFormats.QrCode,
                Multiple = false,
                AutoRotate = true
            }
        };

        _cameraView.BarcodesDetected += CameraView_BarcodesDetected;

        ScannerHost.Children.Clear();
        ScannerHost.Children.Add(_cameraView);
    }

    private void CameraView_BarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (e.Results == null || e.Results.Count == 0)
        {
            return;
        }

        var raw = e.Results[0].Value?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return;
        }

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            if (_cameraView != null)
            {
                _cameraView.IsDetecting = false;
            }

            await HandleRedeemUrlAsync(raw);
        });
    }
#endif

    private async void ManualContinueButton_Clicked(object? sender, EventArgs e)
    {
        var raw = RedeemUrlEntry.Text?.Trim();
        if (string.IsNullOrWhiteSpace(raw))
        {
            StatusLabel.Text = "Paste the redeem URL first.";
            return;
        }

        await HandleRedeemUrlAsync(raw);
    }

    private async Task HandleRedeemUrlAsync(string raw)
    {
        try
        {
            var code = ExtractCodeFromRedeemUrl(raw);
            if (string.IsNullOrWhiteSpace(code))
            {
                StatusLabel.Text = "That QR / redeem URL is not valid.";
                await ShowScannerAsync();
                return;
            }

            StatusLabel.Text = "Redeeming…";

            var redeem = await RedeemAsync(code);
            ShowDob(redeem.SessionId);
        }
        catch
        {
            StatusLabel.Text = "Could not redeem. Try again.";
            await ShowScannerAsync();
        }
    }

    private async void ContinueButton_Clicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_pendingSessionId))
        {
            await ShowScannerAsync();
            return;
        }

        try
        {
            StatusLabel.Text = "Signing in…";

            var dob = new DateOnly(DobPicker.Date.Year, DobPicker.Date.Month, DobPicker.Date.Day);
            var tokens = await IssueTokenAsync(_pendingSessionId, dob);

            await SaveTokensAsync(tokens);

            StatusLabel.Text = "Signed in.";
            await Navigation.PushAsync(new WebShellPage());
        }
        catch
        {
            StatusLabel.Text = "Date of birth did not match. Try again.";
        }
    }

    private async Task<AppAuthRedeemResponse> RedeemAsync(string code)
    {
        var res = await _httpClient.PostAsJsonAsync("api/auth/app/redeem", new AppAuthRedeemRequest { Code = code });
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<AppAuthRedeemResponse>();
        if (body == null || string.IsNullOrWhiteSpace(body.SessionId))
        {
            throw new InvalidOperationException("Redeem response invalid.");
        }

        return body;
    }

    private async Task<AuthTokenResponse> IssueTokenAsync(string sessionId, DateOnly dateOfBirth)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "api/auth/app/token");

        req.Headers.TryAddWithoutValidation(ApiKeyHeaderName, _apiKey);
        req.Headers.TryAddWithoutValidation(ClientIdHeaderName, _clientId);

        req.Content = JsonContent.Create(new AppAuthIssueTokenRequest
        {
            SessionId = sessionId,
            DateOfBirth = dateOfBirth.ToString("yyyy-MM-dd"),
        });

        var res = await _httpClient.SendAsync(req);
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<AuthTokenResponse>();
        if (body == null || string.IsNullOrWhiteSpace(body.AccessToken) || string.IsNullOrWhiteSpace(body.RefreshToken))
        {
            throw new InvalidOperationException("Token response invalid.");
        }

        return body;
    }

    private async Task<AuthTokenResponse> RefreshAsync(string refreshToken)
    {
        using var req = new HttpRequestMessage(HttpMethod.Post, "api/auth/app/refresh");

        req.Headers.TryAddWithoutValidation(ApiKeyHeaderName, _apiKey);
        req.Headers.TryAddWithoutValidation(ClientIdHeaderName, _clientId);

        req.Content = JsonContent.Create(new AuthRefreshRequest { RefreshToken = refreshToken });

        var res = await _httpClient.SendAsync(req);
        res.EnsureSuccessStatusCode();

        var body = await res.Content.ReadFromJsonAsync<AuthTokenResponse>();
        if (body == null || string.IsNullOrWhiteSpace(body.AccessToken) || string.IsNullOrWhiteSpace(body.RefreshToken))
        {
            throw new InvalidOperationException("Refresh response invalid.");
        }

        return body;
    }

    private static async Task SaveTokensAsync(AuthTokenResponse tokens)
    {
        await SecureStorage.SetAsync(StorageRefreshToken, tokens.RefreshToken);
    }

    private static string? ExtractCodeFromRedeemUrl(string raw)
    {
        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var qs = HttpUtility.ParseQueryString(uri.Query);
        var code = qs.Get("code");
        return string.IsNullOrWhiteSpace(code) ? null : code.Trim();
    }

    private sealed class AppAuthRedeemRequest
    {
        public string Code { get; set; } = string.Empty;
    }

    private sealed class AppAuthRedeemResponse
    {
        public string SessionId { get; set; } = string.Empty;
        public DateTimeOffset ExpiresUtc { get; set; }
    }

    private sealed class AppAuthIssueTokenRequest
    {
        public string SessionId { get; set; } = string.Empty;
        public string DateOfBirth { get; set; } = string.Empty;
    }

    private sealed class AuthRefreshRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    private sealed class AuthTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTimeOffset AccessTokenExpiresUtc { get; set; }
        public string RefreshToken { get; set; } = string.Empty;
        public DateTimeOffset RefreshTokenExpiresUtc { get; set; }
    }
}
