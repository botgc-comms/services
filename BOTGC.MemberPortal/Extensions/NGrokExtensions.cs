using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using BOTGC.MemberPortal.Hubs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BOTGC.MemberPortal;

public static class NgrokExtensions
{
    public static void UseNgrokTunnel(this WebApplication app)
    {
        var config = app.Services.GetRequiredService<IConfiguration>();
        var enabled = config.GetValue<bool>("Ngrok:Enable") || config.GetValue<bool>("AppSettings:Ngrok:Enable");
        if (!app.Environment.IsDevelopment() || !enabled) return;

        app.MapGet("/device-link/create", async (HttpContext ctx, string? returnUrl) =>
        {
            if (ctx.User?.Identity?.IsAuthenticated != true)
            {
                return Results.Unauthorized();
            }

            var cache = ctx.RequestServices.GetRequiredService<IDistributedCache>();
            var token = CreateToken();

            var payload = new DeviceLinkPayload
            {
                ReturnUrl = NormaliseReturnUrl(returnUrl),
                Claims = ctx.User.Claims.Select(c => new DeviceLinkClaim { Type = c.Type, Value = c.Value }).ToList()
            };

            var json = JsonSerializer.Serialize(payload);

            await cache.SetStringAsync(
                DeviceLinkKey(token),
                json,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
                });

            return Results.Json(new { path = $"/device-link/consume?t={Uri.EscapeDataString(token)}" });
        });

        app.MapGet("/device-link/consume", async (HttpContext ctx, string? t) =>
        {
            if (string.IsNullOrWhiteSpace(t))
            {
                return Results.Redirect("/Account/Login");
            }

            var cache = ctx.RequestServices.GetRequiredService<IDistributedCache>();
            var key = DeviceLinkKey(t);

            var json = await cache.GetStringAsync(key);
            if (string.IsNullOrWhiteSpace(json))
            {
                return Results.Redirect("/Account/Login");
            }

            await cache.RemoveAsync(key);

            DeviceLinkPayload? payload;
            try
            {
                payload = JsonSerializer.Deserialize<DeviceLinkPayload>(json);
            }
            catch
            {
                return Results.Redirect("/Account/Login");
            }

            if (payload == null || payload.Claims == null || payload.Claims.Count == 0)
            {
                return Results.Redirect("/Account/Login");
            }

            var claims = payload.Claims.Select(c => new System.Security.Claims.Claim(c.Type, c.Value)).ToList();
            var identity = new System.Security.Claims.ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new System.Security.Claims.ClaimsPrincipal(identity);

            await ctx.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = false });

            var ru = payload.ReturnUrl;
            if (!string.IsNullOrWhiteSpace(ru) && ru.StartsWith("/", StringComparison.Ordinal))
            {
                return Results.Redirect(ru);
            }

            return Results.Redirect("/");
        });

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    var killLocal = config.GetValue<bool?>("Ngrok:KillExistingProcesses") == true
                                 || config.GetValue<bool?>("AppSettings:Ngrok:KillExistingProcesses") == true;
                    var apiToken = config["Ngrok:ApiToken"] ?? config["AppSettings:Ngrok:ApiToken"];

                    var targetUri = ResolveListeningAddress(app);

                    var reused = await TryReuseExistingLocalTunnelAsync(app, targetUri);
                    if (reused) return;

                    await StopExistingLocalAgentsAsync(killLocal);

                    if (!string.IsNullOrWhiteSpace(apiToken))
                    {
                        var (stopped, remaining) = await EnsureNoCloudSessionsAsync(apiToken!, TimeSpan.FromSeconds(12));
                        Console.WriteLine($"[ngrok] Cloud cleanup: stopped {stopped} session(s); remaining={remaining}.");
                    }

                    var started = await StartNgrokProcessAsync(app, targetUri);
                    if (!started && !string.IsNullOrWhiteSpace(apiToken))
                    {
                        var (stopped2, remaining2) = await EnsureNoCloudSessionsAsync(apiToken!, TimeSpan.FromSeconds(12));
                        Console.WriteLine($"[ngrok] Retry cleanup: stopped {stopped2} session(s); remaining={remaining2}.");
                        await StopExistingLocalAgentsAsync(true);
                        await StartNgrokProcessAsync(app, targetUri);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ngrok] Failed: {ex.Message}");
                }
            });
        });
    }

    private static string DeviceLinkKey(string token) => $"dev:device-link:{token}";

    private static string CreateToken()
    {
        Span<byte> bytes = stackalloc byte[32];
        System.Security.Cryptography.RandomNumberGenerator.Fill(bytes);

        var s = Convert.ToBase64String(bytes);
        s = s.Replace("+", "-").Replace("/", "_").TrimEnd('=');
        return s;
    }

    private static string NormaliseReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        if (!returnUrl.StartsWith("/", StringComparison.Ordinal))
        {
            return "/" + returnUrl;
        }

        return returnUrl;
    }

    private sealed class DeviceLinkPayload
    {
        public string ReturnUrl { get; set; } = "/";
        public System.Collections.Generic.List<DeviceLinkClaim> Claims { get; set; } = new();
    }

    private sealed class DeviceLinkClaim
    {
        public string Type { get; set; } = "";
        public string Value { get; set; } = "";
    }

    private static async Task<bool> TryReuseExistingLocalTunnelAsync(WebApplication app, Uri targetUri)
    {
        var desiredHttps = string.Equals(targetUri.Scheme, "https", StringComparison.OrdinalIgnoreCase);
        var desiredAddrA = $"{targetUri.Scheme}://localhost:{targetUri.Port}";
        var desiredAddrB = $"localhost:{targetUri.Port}";

        foreach (var port in Enumerable.Range(4040, 6))
        {
            try
            {
                using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}/"), Timeout = TimeSpan.FromMilliseconds(800) };
                var info = await http.GetFromJsonAsync<TunnelApi>("api/tunnels");
                var tunnels = info?.tunnels ?? new System.Collections.Generic.List<TunnelApi.Tunnel>();
                if (tunnels.Count == 0) continue;

                var match = tunnels
                    .Where(t =>
                        !string.IsNullOrWhiteSpace(t.config?.addr) &&
                        (string.Equals(t.config.addr, desiredAddrA, StringComparison.OrdinalIgnoreCase) ||
                         string.Equals(t.config.addr, desiredAddrB, StringComparison.OrdinalIgnoreCase)))
                    .OrderByDescending(t => string.Equals(t.proto, "https", StringComparison.OrdinalIgnoreCase))
                    .ThenBy(t => t.name)
                    .FirstOrDefault();

                if (match == null) continue;

                var publicUrl = !string.IsNullOrWhiteSpace(tunnels.FirstOrDefault(t => string.Equals(t.proto, "https", StringComparison.OrdinalIgnoreCase) && t.name == match.name)?.public_url)
                    ? tunnels.First(t => string.Equals(t.proto, "https", StringComparison.OrdinalIgnoreCase) && t.name == match.name).public_url
                    : match.public_url;

                if (string.IsNullOrWhiteSpace(publicUrl) || IsLocalUrl(publicUrl)) continue;

                publicUrl = publicUrl.TrimEnd('/');
                Console.WriteLine($"[ngrok] Reusing existing tunnel: {publicUrl}  =>  {targetUri}");

                var state = app.Services.GetService<NgrokState>();
                if (state != null) state.PublicUrl = publicUrl;

                try
                {
                    var hub = app.Services.GetService<IHubContext<NgrokHub>>();
                    if (hub != null) { await hub.Clients.All.SendAsync("NgrokUrlAvailable", publicUrl); }
                }
                catch { }

                return true;
            }
            catch { }
        }

        return false;
    }

    private static async Task<bool> StartNgrokProcessAsync(WebApplication app, Uri targetUri)
    {
        var isHttps = string.Equals(targetUri.Scheme, "https", StringComparison.OrdinalIgnoreCase);
        var args = isHttps
            ? $"http https://localhost:{targetUri.Port} --host-header=localhost:{targetUri.Port} --log=stdout --log-format=logfmt"
            : $"http {targetUri.Port} --host-header=rewrite --log=stdout --log-format=logfmt";

        var psi = new ProcessStartInfo
        {
            FileName = "ngrok",
            Arguments = args,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        Process? proc;
        try
        {
            proc = Process.Start(psi);
            if (proc == null)
            {
                Console.WriteLine("[ngrok] Failed to start process.");
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("[ngrok] Start failed: " + ex.Message);
            return false;
        }

        var lines = new ConcurrentQueue<string>();

        _ = Task.Run(async () =>
        {
            try
            {
                while (!proc.HasExited)
                {
                    var line = await proc.StandardOutput.ReadLineAsync();
                    if (line == null) break;
                    lines.Enqueue(line);
                    Console.WriteLine("[ngrok] " + line);
                }
            }
            catch { }
        });

        _ = Task.Run(async () =>
        {
            try
            {
                while (!proc.HasExited)
                {
                    var line = await proc.StandardError.ReadLineAsync();
                    if (line == null) break;
                    lines.Enqueue(line);
                    Console.WriteLine("[ngrok:err] " + line);
                }
            }
            catch { }
        });

        var (urlFromLogs, errFromLogs) = await GetPublicUrlOrErrorAsync(lines, CancellationToken.None);
        if (errFromLogs != null)
        {
            Console.WriteLine("[ngrok] Error code: " + errFromLogs);
            return false;
        }

        var publicUrl = await TryGetPublicUrlFromApisAsync(CancellationToken.None);
        if (string.IsNullOrWhiteSpace(publicUrl)) publicUrl = urlFromLogs;
        if (string.IsNullOrWhiteSpace(publicUrl) || IsLocalUrl(publicUrl))
        {
            publicUrl = await TryGetPublicUrlFromApisAsync(CancellationToken.None);
        }

        if (string.IsNullOrWhiteSpace(publicUrl) || IsLocalUrl(publicUrl))
        {
            Console.WriteLine("[ngrok] Failed to resolve public URL. Parsed only local addresses.");
            return false;
        }

        publicUrl = publicUrl.TrimEnd('/');
        Console.WriteLine($"[ngrok] {publicUrl}  =>  {targetUri}");

        var state = app.Services.GetService<NgrokState>();
        if (state != null) state.PublicUrl = publicUrl;

        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
        lifetime.ApplicationStopping.Register(() =>
        {
            try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); } catch { }
        });

        try
        {
            var hub = app.Services.GetService<IHubContext<NgrokHub>>();
            if (hub != null) { await hub.Clients.All.SendAsync("NgrokUrlAvailable", publicUrl); }
        }
        catch { }

        return true;
    }

    private static Uri ResolveListeningAddress(WebApplication app)
    {
        var server = app.Services.GetRequiredService<IServer>();
        var feat = server.Features.Get<IServerAddressesFeature>();
        var addrs = (feat?.Addresses ?? Array.Empty<string>()).ToList();

        if (addrs.Count == 0 && app.Urls.Any()) addrs = app.Urls.ToList();

        if (addrs.Count == 0)
        {
            var cfg = app.Services.GetRequiredService<IConfiguration>();
            var urls = cfg["ASPNETCORE_URLS"] ?? cfg["urls"];
            if (!string.IsNullOrWhiteSpace(urls))
            {
                addrs = urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
            }
        }

        if (addrs.Count == 0) throw new InvalidOperationException("Could not determine listening addresses.");

        var target = addrs.FirstOrDefault(u => u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                  ?? addrs.FirstOrDefault(u => uStartsWithHttp(u))
                  ?? addrs[0];

        if (!Uri.TryCreate(target, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException($"Invalid address: '{target}'.");
        }

        return uri;

        static bool uStartsWithHttp(string u) => u.StartsWith("http://", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task StopExistingLocalAgentsAsync(bool killLocalProcesses)
    {
        foreach (var port in Enumerable.Range(4040, 6))
        {
            try
            {
                using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}/"), Timeout = TimeSpan.FromSeconds(2) };

                try
                {
                    var sess = await http.GetFromJsonAsync<SessionsApi>("api/sessions");
                    if (sess?.sessions != null)
                    {
                        foreach (var s in sess.sessions)
                        {
                            try { await http.DeleteAsync($"api/sessions/{Uri.EscapeDataString(s.id)}"); } catch { }
                        }
                    }
                }
                catch { }

                try
                {
                    var tuns = await http.GetFromJsonAsync<TunnelApi>("api/tunnels");
                    if (tuns?.tunnels != null)
                    {
                        foreach (var t in tuns.tunnels)
                        {
                            try { await http.DeleteAsync($"api/tunnels/{Uri.EscapeDataString(t.name)}"); } catch { }
                        }
                    }
                }
                catch { }
            }
            catch { }
        }

        if (!killLocalProcesses) return;

        try
        {
            foreach (var p in Process.GetProcessesByName("ngrok"))
            {
                try { p.Kill(entireProcessTree: true); } catch { }
            }
        }
        catch { }
    }

    private static async Task<(int stopped, int remaining)> EnsureNoCloudSessionsAsync(string apiToken, TimeSpan timeout)
    {
        var stoppedTotal = 0;
        var sw = Stopwatch.StartNew();

        while (sw.Elapsed < timeout)
        {
            var sessions = await ListTunnelSessionsAsync(apiToken);
            if (sessions.Length == 0) return (stoppedTotal, 0);

            var stoppedThisPass = 0;
            foreach (var id in sessions)
            {
                if (await StopTunnelSessionAsync(apiToken, id)) stoppedThisPass++;
            }
            stoppedTotal += stoppedThisPass;

            await Task.Delay(500);
        }

        var remaining = (await ListTunnelSessionsAsync(apiToken)).Length;
        return (stoppedTotal, remaining);
    }

    private static async Task<string[]> ListTunnelSessionsAsync(string apiToken)
    {
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri("https://api.ngrok.com/") };
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiToken);
            http.DefaultRequestHeaders.Add("Ngrok-Version", "2");

            var ids = new System.Collections.Generic.List<string>();
            string? next = "tunnel_sessions?limit=100";

            while (!string.IsNullOrWhiteSpace(next))
            {
                var resp = await http.GetAsync(next);
                if (!resp.IsSuccessStatusCode) break;

                using var doc = JsonDocument.Parse(await resp.Content.ReadAsStringAsync());
                if (doc.RootElement.TryGetProperty("tunnel_sessions", out var arr))
                {
                    foreach (var el in arr.EnumerateArray())
                    {
                        var id = el.TryGetProperty("id", out var idEl) ? idEl.GetString() : null;
                        if (!string.IsNullOrWhiteSpace(id)) ids.Add(id!);
                    }
                }

                next = null;
                if (doc.RootElement.TryGetProperty("next_page_uri", out var nextEl))
                {
                    var nv = nextEl.GetString();
                    if (!string.IsNullOrWhiteSpace(nv)) next = nv.TrimStart('/');
                }
            }

            return ids.ToArray();
        }
        catch { return Array.Empty<string>(); }
    }

    private static async Task<bool> StopTunnelSessionAsync(string apiToken, string id)
    {
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri("https://api.ngrok.com/") };
            http.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiToken);
            http.DefaultRequestHeaders.Add("Ngrok-Version", "2");

            using var body = new StringContent("{}", Encoding.UTF8, "application/json");
            var resp = await http.PostAsync($"tunnel_sessions/{Uri.EscapeDataString(id)}/stop", body);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    private static async Task<(string? PublicUrl, string? ErrorCode)> GetPublicUrlOrErrorAsync(ConcurrentQueue<string> logLines, CancellationToken ct)
    {
        var urlFieldRx = new Regex(@"\burl=(https://[^\s""']+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var httpsRx = new Regex(@"https://[^\s""']+", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var errRx = new Regex(@"\bERR_NGROK_(\d+)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        var start = DateTime.UtcNow;
        while ((DateTime.UtcNow - start) < TimeSpan.FromSeconds(30) && !ct.IsCancellationRequested)
        {
            while (logLines.TryDequeue(out var line))
            {
                var em = errRx.Match(line);
                if (em.Success) return (null, "ERR_NGROK_" + em.Groups[1].Value);

                var fm = urlFieldRx.Match(line);
                if (fm.Success)
                {
                    var candidate = fm.Groups[1].Value;
                    if (!IsLocalUrl(candidate)) return (candidate, null);
                }

                foreach (Match m in httpsRx.Matches(line))
                {
                    var candidate = m.Value;
                    if (!IsLocalUrl(candidate)) return (candidate, null);
                }
            }
            await Task.Delay(150, ct);
        }
        return (null, null);
    }

    private static async Task<string?> TryGetPublicUrlFromApisAsync(CancellationToken ct)
    {
        foreach (var port in Enumerable.Range(4040, 6))
        {
            try
            {
                using var http = new HttpClient { BaseAddress = new Uri($"http://127.0.0.1:{port}/"), Timeout = TimeSpan.FromMilliseconds(700) };
                for (var i = 0; i < 10 && !ct.IsCancellationRequested; i++)
                {
                    try
                    {
                        var info = await http.GetFromJsonAsync<TunnelApi>("api/tunnels", ct);
                        var url = info?.tunnels?.FirstOrDefault(t => t.proto == "https")?.public_url
                                  ?? info?.tunnels?.FirstOrDefault()?.public_url;
                        if (!string.IsNullOrWhiteSpace(url) && !IsLocalUrl(url)) return url;
                    }
                    catch { }
                    await Task.Delay(200, ct);
                }
            }
            catch { }
        }
        return null;
    }

    private static bool IsLocalUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var u)) return true;
        if (string.Equals(u.Host, "localhost", StringComparison.OrdinalIgnoreCase)) return true;
        if (u.Host.StartsWith("127.", StringComparison.OrdinalIgnoreCase)) return true;
        if (u.Host.Equals("::1", StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    private sealed class SessionsApi
    {
        public System.Collections.Generic.List<Session> sessions { get; set; } = new();
        public sealed class Session { public string id { get; set; } = ""; }
    }

    private sealed class TunnelApi
    {
        public System.Collections.Generic.List<Tunnel> tunnels { get; set; } = new();
        public sealed class Tunnel
        {
            public string name { get; set; } = "";
            public string public_url { get; set; } = "";
            public string proto { get; set; } = "";
            public Config? config { get; set; } = null;
        }

        public sealed class Config
        {
            public string addr { get; set; } = "";
        }
    }
}

public sealed class NgrokState
{
    public string? PublicUrl { get; set; }
}
