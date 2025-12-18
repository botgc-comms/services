using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace BOTGC.Mobile.Pages;

public partial class MetallicSplashPage : ContentPage
{
    private const float FadeInSeconds = 0.8f;

    private SKBitmap? _logo;
    private float _t;
    private bool _running;

    public MetallicSplashPage()
    {
        InitializeComponent();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        if (_running)
        {
            return;
        }

        _running = true;
        _ = RunAsync();
    }

    protected override void OnDisappearing()
    {
        _running = false;
        base.OnDisappearing();
    }

    private async Task RunAsync()
    {
        if (_logo is null)
        {
            await using var s = await FileSystem.OpenAppPackageFileAsync("splash_crest_ds.png");
            _logo = SKBitmap.Decode(s);
        }

        var start = DateTimeOffset.UtcNow;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        while (_running)
        {
            var seconds = (float)sw.Elapsed.TotalSeconds;
            _t = seconds % 1.6f;

            MainThread.BeginInvokeOnMainThread(() => Canvas.InvalidateSurface());

            if (DateTimeOffset.UtcNow - start >= TimeSpan.FromSeconds(5))
            {
                _running = false;

                MainThread.BeginInvokeOnMainThread(() =>
                {
                    Application.Current!.MainPage!.Navigation.PushAsync(new AppAuthPage());
                });

                return;
            }

            await Task.Delay(16);
        }
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.White);

        if (_logo is null)
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

        var fade = Math.Clamp(_t / FadeInSeconds, 0f, 1f);
        var alpha = (byte)(fade * 255f);

        //// Drop shadow (behind logo)
        //using (var shadowPaint = new SKPaint
        //{
        //    IsAntialias = true,
        //    ImageFilter = SKImageFilter.CreateDropShadow(
        //        dx: 0,
        //        dy: 14,
        //        sigmaX: 22,
        //        sigmaY: 22,
        //        color: new SKColor(0, 0, 0, (byte)(alpha * 0.6f))
        //    )
        //})
        //{
        //    canvas.DrawBitmap(_logo, dest, shadowPaint);
        //}

        // Logo with fade-in
        using (var logoPaint = new SKPaint
        {
            IsAntialias = true,
            FilterQuality = SKFilterQuality.High,
            Color = new SKColor(255, 255, 255, alpha)
        })
        {
            canvas.DrawBitmap(_logo, dest, logoPaint);
        }

        // Metallic sweep (masked to logo alpha)
        using var brighten = new SKPaint
        {
            BlendMode = SKBlendMode.Screen,
            IsAntialias = true,
            Color = new SKColor(255, 255, 255, alpha)
        };

        canvas.SaveLayer(dest, null);

        brighten.Shader = BuildMetallicSweepShader(dest, _t);
        canvas.DrawRect(dest, brighten);

        using var alphaLayerPaint = new SKPaint { BlendMode = SKBlendMode.DstIn };
        canvas.DrawBitmap(_logo, dest, alphaLayerPaint);

        canvas.Restore();
    }

    private static SKShader BuildMetallicSweepShader(SKRect dest, float t)
    {
        var startX = dest.Left - dest.Width * 0.8f;
        var endX = dest.Right + dest.Width * 0.8f;

        var sweepX = Lerp(startX, endX, EaseInOut(t / 1.6f));

        var p0 = new SKPoint(sweepX - dest.Width * 0.35f, dest.Top);
        var p1 = new SKPoint(sweepX + dest.Width * 0.35f, dest.Bottom);

        var colours = new[]
        {
            new SKColor(255, 255, 255, 0),
            new SKColor(255, 255, 255, 35),
            new SKColor(255, 255, 255, 210),
            new SKColor(255, 255, 255, 35),
            new SKColor(255, 255, 255, 0)
        };

        var stops = new[] { 0f, 0.35f, 0.5f, 0.65f, 1f };

        return SKShader.CreateLinearGradient(p0, p1, colours, stops, SKShaderTileMode.Clamp);
    }

    private static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * t;
    }

    private static float EaseInOut(float t)
    {
        t = Math.Clamp(t, 0f, 1f);
        return t * t * (3f - 2f * t);
    }
}
