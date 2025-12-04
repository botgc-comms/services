using Microsoft.Extensions.Options;
using System.Net;
using BOTGC.POS;
using BOTGC.POS.Common;

namespace BOTGC.POS.Services;

public sealed class GatekeeperOptions
{
    public string QueryKey { get; set; } = "k";
    public string SharedSecret { get; set; } = "";
    public string CookieName { get; set; } = "pos_access";
    public string? RedirectUrl { get; set; } // e.g. "/access?returnUrl={returnUrl}"
    public string[] AllowedCidrs { get; set; } = Array.Empty<string>();
}

public sealed class GatekeeperMiddleware
{
    private readonly RequestDelegate _next;
    private readonly GatekeeperOptions _opts;

    private static readonly string[] PreviewBots = new[]
    {
        "facebookexternalhit", "Facebot",
        "WhatsApp", "Twitterbot", "Slackbot-LinkExpanding",
        "LinkedInBot", "Discordbot"
    };

    public GatekeeperMiddleware(RequestDelegate next, GatekeeperOptions opts)
    {
        _next = next;
        _opts = opts;
    }
    private static bool IsPreviewCrawler(HttpContext ctx)
    {
        var ua = ctx.Request.Headers.UserAgent.ToString();
        if (string.IsNullOrEmpty(ua)) return false;
        foreach (var b in PreviewBots)
            if (ua.Contains(b, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    public async Task Invoke(HttpContext ctx)
    {
        // BYPASS: allow the access endpoint (and its static assets if any)
        if (ctx.Request.Path.StartsWithSegments("/access", StringComparison.OrdinalIgnoreCase))
        {
            await _next(ctx);
            return;
        }

        // Let link-preview crawlers see the page + images so previews aren't blurry
        if (IsPreviewCrawler(ctx) && (HttpMethods.IsGet(ctx.Request.Method) || HttpMethods.IsHead(ctx.Request.Method)))
        {
            await _next(ctx);
            return;
        }

        // (optional) Always allow static images/css/js to load without a key
        if (ctx.Request.Path.StartsWithSegments("/img", StringComparison.OrdinalIgnoreCase) ||
            ctx.Request.Path.StartsWithSegments("/css", StringComparison.OrdinalIgnoreCase) ||
            ctx.Request.Path.StartsWithSegments("/manifest", StringComparison.OrdinalIgnoreCase) ||
            ctx.Request.Path.StartsWithSegments("/voucher", StringComparison.OrdinalIgnoreCase) ||
            ctx.Request.Path.StartsWithSegments("/js", StringComparison.OrdinalIgnoreCase))
        {
            await _next(ctx);
            return;
        }

        // Allow if whitelisted IP/CIDR or token is valid
        if (IsWhitelisted(ctx) || HasValidToken(ctx))
        {
            await _next(ctx);
            return;
        }

        // Redirect browser page loads to /access; never redirect SignalR/API
        if (WantsHtml(ctx) && !IsSignalR(ctx) && !string.IsNullOrEmpty(_opts.RedirectUrl))
        {
            var returnUrl = Uri.EscapeDataString(ctx.Request.Path + ctx.Request.QueryString);
            var target = _opts.RedirectUrl!.Replace("{returnUrl}", returnUrl);
            ctx.Response.Redirect(target, false);
            return;
        }

        ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
        await ctx.Response.WriteAsync("Forbidden");
    }

    private bool HasValidToken(HttpContext ctx)
    {
        if (string.IsNullOrWhiteSpace(_opts.SharedSecret))
            return false;

        // 1) Cookie
        if (ctx.Request.Cookies.TryGetValue(_opts.CookieName, out var cv) && cv == _opts.SharedSecret)
            return true;

        // 2) Query string (set cookie for next requests)
        if (ctx.Request.Query.TryGetValue(_opts.QueryKey, out var qv) && qv == _opts.SharedSecret)
        {
            ctx.Response.Cookies.Append(
                _opts.CookieName,
                _opts.SharedSecret,
                new CookieOptions
                {
                    HttpOnly = true,
                    Secure = ctx.Request.IsHttps,
                    SameSite = SameSiteMode.Lax,
                    Path = "/",
                    Expires = DateTimeOffset.UtcNow.AddDays(180)
                }
            );
            return true;
        }

        // 3) Optional header for server-to-server calls
        if (ctx.Request.Headers.TryGetValue("X-Access-Key", out var hv) && hv == _opts.SharedSecret)
            return true;

        return false;
    }

    private static bool WantsHtml(HttpContext ctx)
    {
        var accept = ctx.Request.Headers["Accept"].ToString();
        return accept.Contains("text/html", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSignalR(HttpContext ctx)
        => ctx.Request.Path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase);

    private bool IsWhitelisted(HttpContext ctx)
    {
        if (_opts.AllowedCidrs.Length == 0) return false;
        var ip = ctx.Connection.RemoteIpAddress;
        if (ip is null) return false;

        foreach (var cidr in _opts.AllowedCidrs)
        {
            if (TryParseCidr(cidr, out var network, out var mask) && IsInSubnet(ip, network, mask))
                return true;
        }
        return false;
    }

    private static bool TryParseCidr(string cidr, out IPAddress network, out IPAddress mask)
    {
        network = IPAddress.None; mask = IPAddress.None;
        var parts = cidr.Split('/');
        if (parts.Length != 2) return false;
        if (!IPAddress.TryParse(parts[0], out network)) return false;
        if (!int.TryParse(parts[1], out var prefix)) return false;
        if (network.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork) return false;
        var maskBytes = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(-1 << (32 - prefix)));
        if (BitConverter.IsLittleEndian) Array.Reverse(maskBytes);
        mask = new IPAddress(maskBytes);
        return true;
    }

    private static bool IsInSubnet(IPAddress addr, IPAddress network, IPAddress mask)
    {
        var a = addr.GetAddressBytes();
        var n = network.GetAddressBytes();
        var m = mask.GetAddressBytes();
        if (a.Length != n.Length || n.Length != m.Length) return false;
        for (int i = 0; i < a.Length; i++)
            if ((a[i] & m[i]) != (n[i] & m[i])) return false;
        return true;
    }
}
