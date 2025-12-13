using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace BOTGC.Mobile;

public partial class MetallicSplashPage : ContentPage
{
    private SKBitmap? _logo;
    private float _t;
    private bool _running;

    public MetallicSplashPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_logo is null)
        {
            await using var s = await FileSystem.OpenAppPackageFileAsync("splash_crest.png");
            _logo = SKBitmap.Decode(s);
        }

        if (_running)
        {
            return;
        }

        _running = true;

        _ = Task.Run(async () =>
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();

            while (_running)
            {
                var seconds = (float)sw.Elapsed.TotalSeconds;
                _t = seconds % 1.6f;

                MainThread.BeginInvokeOnMainThread(() => Canvas.InvalidateSurface());

                await Task.Delay(16);
            }
        });
    }

    protected override void OnDisappearing()
    {
        _running = false;
        base.OnDisappearing();
    }

    private void OnPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Black);

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

        canvas.DrawBitmap(_logo, dest);

        using var logoAlpha = new SKPaint
        {
            BlendMode = SKBlendMode.DstIn
        };

        using var brighten = new SKPaint
        {
            BlendMode = SKBlendMode.Screen,
            IsAntialias = true
        };

        canvas.SaveLayer(dest, null);

        var sweep = BuildMetallicSweepShader(dest, _t);
        brighten.Shader = sweep;
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
