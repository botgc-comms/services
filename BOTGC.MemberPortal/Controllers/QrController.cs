using System.Text;
using Microsoft.AspNetCore.Mvc;
using QRCoder;

namespace BOTGC.MemberPortal.Controllers;

public class QrController : Controller
{
    private readonly NgrokState _ngrok;
    private readonly IWebHostEnvironment _env;

    public QrController(NgrokState ngrok, IWebHostEnvironment env)
    {
        _ngrok = ngrok;
        _env = env;
    }

    [HttpGet("/qr/page.png")]
    public IActionResult Page([FromQuery] string? pathAndQuery, [FromQuery] int? size)
    {
        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        if (_env.IsDevelopment() && !string.IsNullOrWhiteSpace(_ngrok.PublicUrl))
        {
            baseUrl = _ngrok.PublicUrl!;
        }

        var rel = string.IsNullOrWhiteSpace(pathAndQuery) ? "/" : pathAndQuery!;
        var effectiveUrl = new Uri(new Uri(baseUrl), rel).ToString();

        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(effectiveUrl, QRCodeGenerator.ECCLevel.Q);

        var ppm = Math.Clamp(size.GetValueOrDefault(8), 4, 24);

        var dark = new byte[] { 0, 0, 0, 255 };       // black, opaque
        var light = new byte[] { 0, 0, 0, 0 };        // transparent

        var png = new PngByteQRCode(data).GetGraphic(ppm, dark, light, true);

        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate, max-age=0";
        Response.Headers.Pragma = "no-cache";
        Response.Headers.Expires = "0";

        return File(png, "image/png");
    }
}
