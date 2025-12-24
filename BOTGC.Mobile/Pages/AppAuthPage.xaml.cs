// Pages/AppAuthPage.xaml.cs
using BOTGC.Mobile.Interfaces;
using Microsoft.Maui.ApplicationModel;

#if ANDROID || IOS || MACCATALYST
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;
#endif

namespace BOTGC.Mobile.Pages;

public partial class AppAuthPage : ContentPage
{
    private readonly IAppAuthService _authService;

    private bool _isRedeeming;

#if ANDROID || IOS || MACCATALYST
    private CameraBarcodeReaderView? _cameraView;
    private bool _scannerInitialised;
#endif

    public AppAuthPage(IAppAuthService authService)
    {
        _authService = authService;

        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await ShowScannerAsync();
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

    private async Task ShowScannerAsync()
    {
        _isRedeeming = false;

        ScannerHelpLabel.Text = "Use the camera to scan your QR code.";
        ManualPanel.IsVisible = false;

#if ANDROID || IOS || MACCATALYST
        if (!ZXing.Net.Maui.BarcodeScanning.IsSupported)
        {
            ScannerHelpLabel.Text = "Camera scanning is not available here. Paste the redeem URL instead.";
            ManualPanel.IsVisible = true;
            StatusLabel.Text = "Paste the redeem URL to continue.";
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
            StatusLabel.Text = "Paste the redeem URL to continue.";
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
                AutoRotate = true,
            },
        };

        _cameraView.BarcodesDetected += CameraView_BarcodesDetected;

        ScannerHost.Children.Clear();
        ScannerHost.Children.Add(_cameraView);
    }

    private void CameraView_BarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
    {
        if (_isRedeeming)
        {
            return;
        }

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
        if (_isRedeeming)
        {
            return;
        }

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
        if (_isRedeeming)
        {
            return;
        }

        _isRedeeming = true;

        try
        {
            var code = _authService.ExtractCodeFromRedeemUrl(raw);
            if (string.IsNullOrWhiteSpace(code))
            {
                StatusLabel.Text = "That QR / redeem URL is not valid.";
                await ShowScannerAsync();
                return;
            }

            StatusLabel.Text = "Redeeming…";

            var redeem = await _authService.RedeemAsync(code);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                var services = Application.Current!.Handler!.MauiContext!.Services;

                var dobPage = services.GetRequiredService<DateOfBirthPage>();
                dobPage.SetSessionId(redeem.SessionId);

                if (Application.Current!.MainPage is NavigationPage nav)
                {
                    nav.Navigation.PushAsync(dobPage);
                }
                else
                {
                    Application.Current!.MainPage = new NavigationPage(dobPage);
                }
            });
        }
        catch
        {
            StatusLabel.Text = "Could not redeem. Try again.";
            await ShowScannerAsync();
        }
        finally
        {
            _isRedeeming = false;
        }
    }
}
