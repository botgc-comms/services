using System.Diagnostics;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BOTGC.POS;

public static class NGrokExtensions
{
    public static void UseNgrokTunnel(this WebApplication app)
    {
        var config = app.Services.GetRequiredService<IConfiguration>();
        var enabled = config.GetValue<bool>("Ngrok:Enable") || config.GetValue<bool>("AppSettings:Ngrok:Enable");
        if (!app.Environment.IsDevelopment() || !enabled) return;

        app.Lifetime.ApplicationStarted.Register(() =>
        {
            _ = Task.Run(async () =>
            {
                await StopExistingNgrokAsync(app, allowProcessKill:
                    config.GetValue<bool?>("Ngrok:KillExistingProcesses") == true ||
                    config.GetValue<bool?>("AppSettings:Ngrok:KillExistingProcesses") == true);

                var targetUri = ResolveListeningAddress(app);
                var region = config["Ngrok:Region"] ?? config["AppSettings:Ngrok:Region"] ?? "eu";

                var args = targetUri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase)
                    ? $"http https://localhost:{targetUri.Port} --region {region} --host-header=localhost:{targetUri.Port} --log=stdout"
                    : $"http {targetUri.Port} --region {region} --host-header=rewrite --log=stdout";

                var psi = new ProcessStartInfo
                {
                    FileName = "ngrok",
                    Arguments = args,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                var proc = Process.Start(psi);

                var publicUrl = await TryGetPublicUrlAsync();
                if (!string.IsNullOrWhiteSpace(publicUrl))
                {
                    Console.WriteLine($"[ngrok] {publicUrl}  =>  {targetUri}");
                    var state = app.Services.GetService<NgrokState>();
                    if (state != null) state.PublicUrl = publicUrl;
                }

                var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
                lifetime.ApplicationStopping.Register(() =>
                {
                    try { if (proc != null && !proc.HasExited) proc.Kill(); } catch { }
                });
            });
        });
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
                addrs = urls.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        }

        if (addrs.Count == 0) throw new InvalidOperationException("Could not determine listening addresses.");

        var target = addrs.FirstOrDefault(u => u.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                 ?? addrs.FirstOrDefault(u => u.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
                 ?? addrs[0];

        if (!Uri.TryCreate(target, UriKind.Absolute, out var uri))
            throw new InvalidOperationException($"Invalid address: '{target}'.");

        return uri;
    }

    private static async Task StopExistingNgrokAsync(WebApplication app, bool allowProcessKill)
    {
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:4040/"), Timeout = TimeSpan.FromSeconds(2) };
            var info = await http.GetFromJsonAsync<TunnelApi>("api/tunnels");
            if (info?.tunnels is { Count: > 0 })
            {
                foreach (var t in info.tunnels)
                {
                    try
                    {
                        await http.DeleteAsync($"api/tunnels/{Uri.EscapeDataString(t.name)}");
                        Console.WriteLine($"[ngrok] Closed tunnel '{t.name}' via API.");
                    }
                    catch { }
                }
            }
        }
        catch { }

        if (!allowProcessKill) return;

        try
        {
            foreach (var p in Process.GetProcessesByName("ngrok"))
            {
                try
                {
                    p.Kill(entireProcessTree: true);
                    Console.WriteLine($"[ngrok] Killed stray ngrok process (PID {p.Id}).");
                }
                catch { }
            }
        }
        catch { }
    }

    private static async Task<string?> TryGetPublicUrlAsync()
    {
        try
        {
            using var http = new HttpClient { BaseAddress = new Uri("http://127.0.0.1:4040/") };
            for (var i = 0; i < 60; i++)
            {
                await Task.Delay(1000);
                try
                {
                    var info = await http.GetFromJsonAsync<TunnelApi>("api/tunnels");
                    var url = info?.tunnels?.FirstOrDefault(t => t.proto == "https")?.public_url
                              ?? info?.tunnels?.FirstOrDefault()?.public_url;
                    if (!string.IsNullOrWhiteSpace(url)) return url;
                }
                catch { }
            }
        }
        catch { }
        return null;
    }

    private sealed class TunnelApi
    {
        public List<Tunnel> tunnels { get; set; } = new();
        public sealed class Tunnel
        {
            public string name { get; set; } = "";
            public string public_url { get; set; } = "";
            public string proto { get; set; } = "";
        }
    }
}

public sealed class NgrokState
{
    public string? PublicUrl { get; set; }
}
