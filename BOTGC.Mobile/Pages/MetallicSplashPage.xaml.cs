// Pages/MetallicSplashPage.xaml.cs
using BOTGC.Mobile.Interfaces;
using BOTGC.Mobile.Services;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using System.ComponentModel;

namespace BOTGC.Mobile.Pages;

public partial class MetallicSplashPage : ContentPage
{
    private const float FadeInSeconds = 0.8f;
    private const float SweepSeconds = 1.6f;
    private static readonly TimeSpan MinimumSplashDuration = TimeSpan.FromSeconds(4);
    private static readonly TimeSpan InitialBlankDuration = TimeSpan.FromMilliseconds(350);

    private float _fadeT;
    private float _sweepT;
    private float _elapsed;

    private readonly IAppAuthService _authService;
    private readonly INavigationGate _navGate;

    private SKBitmap? _logo;
    private bool _running;
    private CancellationTokenSource? _loopCts;

    public MetallicSplashPage(IAppAuthService authService, INavigationGate navGate)
    {
        _authService = authService;
        _navGate = navGate;

        _fadeT = 0f;
        _sweepT = 0f;
        _elapsed = -(float)InitialBlankDuration.TotalSeconds;

        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        Application.Current!.PropertyChanged -= App_PropertyChanged;
        Application.Current!.PropertyChanged += App_PropertyChanged;

        if (_running)
        {
            return;
        }

        _running = true;
        _loopCts = new CancellationTokenSource();
        _ = RunAsync(_loopCts.Token);
    }

    protected override void OnDisappearing()
    {
        StopLoop();

        Application.Current!.PropertyChanged -= App_PropertyChanged;

        base.OnDisappearing();
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();

        if (Parent is null)
        {
            StopLoop();
        }
    }

    private void App_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(Application.MainPage))
        {
            return;
        }

        if (MainThread.IsMainThread)
        {
            StopIfNoLongerVisible();
            return;
        }

        MainThread.BeginInvokeOnMainThread(StopIfNoLongerVisible);
    }

    private void StopIfNoLongerVisible()
    {
        if (!_running)
        {
            return;
        }

        if (!IsCurrentSplashPage())
        {
            StopLoop();
        }
    }

    private bool IsCurrentSplashPage()
    {
        var main = Application.Current?.MainPage;

        if (main is NavigationPage nav)
        {
            return ReferenceEquals(nav.CurrentPage, this);
        }

        return ReferenceEquals(main, this);
    }

    private void StopLoop()
    {
        _running = false;

        try
        {
            _loopCts?.Cancel();
        }
        catch
        {
        }

        _loopCts?.Dispose();
        _loopCts = null;
    }

    private async Task RunAsync(CancellationToken token)
    {
        if (_logo is null)
        {
            await using var s = await FileSystem.OpenAppPackageFileAsync("splash_crest_ds.png");
            _logo = SKBitmap.Decode(s);
        }

        using var authCts = CancellationTokenSource.CreateLinkedTokenSource(token);
        authCts.CancelAfter(TimeSpan.FromSeconds(30));

        var authTask = _authService.CheckAndRefreshIfPossibleAsync(authCts.Token);
        var minDelayTask = Task.Delay(MinimumSplashDuration, token);
        var allDone = Task.WhenAll(authTask, minDelayTask);

        var sw = System.Diagnostics.Stopwatch.StartNew();

        while (_running && !token.IsCancellationRequested && !allDone.IsCompleted)
        {
            if (!IsCurrentSplashPage())
            {
                StopLoop();
                return;
            }

            _elapsed = (float)sw.Elapsed.TotalSeconds - (float)InitialBlankDuration.TotalSeconds;

            if (_elapsed <= 0f)
            {
                _fadeT = 0f;
                _sweepT = 0f;
            }
            else
            {
                _fadeT = EaseInOut(Math.Clamp(_elapsed / FadeInSeconds, 0f, 1f));

                var sweepTime = Math.Max(0f, _elapsed - FadeInSeconds);
                _sweepT = sweepTime % SweepSeconds;
            }

            InvalidateCanvasSafely();

            try
            {
                await Task.Delay(16, token);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }

        if (!_running || token.IsCancellationRequested)
        {
            return;
        }

        if (!IsCurrentSplashPage())
        {
            StopLoop();
            return;
        }

        if (_navGate.IsTaken)
        {
            StopLoop();
            return;
        }

        if (!_navGate.TryTake())
        {
            StopLoop();
            return;
        }

        IAppAuthService.AuthCheckResult authResult;
        try
        {
            authResult = await authTask;
        }
        catch
        {
            authResult = new IAppAuthService.AuthCheckResult(false, "Auth check failed.");
        }

        StopLoop();

        MainThread.BeginInvokeOnMainThread(() =>
        {
            var services = Application.Current?.Handler?.MauiContext?.Services;
            if (services is null)
            {
                return;
            }

            Page root = authResult.IsAuthenticated
                ? services.GetRequiredService<WebShellPage>()
                : services.GetRequiredService<PairingInfoPage>();

            Application.Current!.MainPage = new NavigationPage(root);
        });
    }

    private void InvalidateCanvasSafely()
    {
        if (!_running)
        {
            return;
        }

        if (!MainThread.IsMainThread)
        {
            MainThread.BeginInvokeOnMainThread(InvalidateCanvasSafely);
            return;
        }

        var view = Canvas;
        if (view is null)
        {
            return;
        }

        if (view.Handler is null)
        {
            return;
        }

        if (view.Handler.PlatformView is null)
        {
            return;
        }

        try
        {
            view.InvalidateSurface();
        }
        catch
        {
        }
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(new SKColor(248, 248, 248));

        if (_logo is null)
        {
            return;
        }

        if (_elapsed <= 0f || _fadeT <= 0f)
        {
            return;
        }

        var w = e.Info.Width;
        var h = e.Info.Height;

        var targetLogoWidth = (int)(Math.Min(w, h) * 0.55f);
        var scale = targetLogoWidth / (float)_logo.Width;
        var targetLogoHeight = (int)(_logo.Height * scale);

        var x = (w - targetLogoWidth) / 2f;
        var y = (h - targetLogoHeight) / 2f;

        var dest = new SKRect(x, y, x + targetLogoWidth, y + targetLogoHeight);

        var alpha = (byte)(_fadeT * 255f);

        using (var logoPaint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High,
            Color = SKColors.White.WithAlpha(alpha),
            BlendMode = SKBlendMode.SrcOver,
        })
        {
            canvas.DrawBitmap(_logo, dest, logoPaint);
        }

        if (_fadeT < 1f)
        {
            return;
        }

        using var brighten = new SKPaint
        {
            BlendMode = SKBlendMode.Screen,
            IsAntialias = true,
            Color = SKColors.White.WithAlpha(255),
        };

        canvas.SaveLayer(dest, null);

        brighten.Shader = BuildMetallicSweepShader(dest, _sweepT);
        canvas.DrawRect(dest, brighten);

        using var alphaLayerPaint = new SKPaint { BlendMode = SKBlendMode.DstIn };
        canvas.DrawBitmap(_logo, dest, alphaLayerPaint);

        canvas.Restore();
    }

    private static SKShader BuildMetallicSweepShader(SKRect dest, float sweepT)
    {
        var startX = dest.Left - dest.Width * 0.8f;
        var endX = dest.Right + dest.Width * 0.8f;

        var sweepX = Lerp(startX, endX, EaseInOut(sweepT / SweepSeconds));

        var p0 = new SKPoint(sweepX - dest.Width * 0.35f, dest.Top);
        var p1 = new SKPoint(sweepX + dest.Width * 0.35f, dest.Bottom);

        var colours = new[]
        {
            new SKColor(255, 255, 255, 0),
            new SKColor(255, 255, 255, 35),
            new SKColor(255, 255, 255, 210),
            new SKColor(255, 255, 255, 35),
            new SKColor(255, 255, 255, 0),
        };

        var stops = new[] { 0f, 0.35f, 0.5f, 0.65f, 1f };

        return SKShader.CreateLinearGradient(p0, p1, colours, stops, SKShaderTileMode.Clamp);
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

    private static float EaseInOut(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
