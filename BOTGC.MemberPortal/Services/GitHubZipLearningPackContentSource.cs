// LearningPackServices.cs
using System.Buffers;
using System.IO.Compression;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BOTGC.MemberPortal.Common;

namespace BOTGC.MemberPortal.Services;

public sealed class GitHubZipLearningPackContentSource : ILearningPackContentSource
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly HttpClient _http;
    private readonly AppSettings _settings;

    public GitHubZipLearningPackContentSource(HttpClient http, AppSettings settings)
    {
        _http = http;
        _settings = settings;
    }

    public async Task<LearningPackContentSnapshot> LoadAsync(CancellationToken ct = default)
    {
        var gh = _settings.LearningPacks.GitHub ?? throw new InvalidOperationException("AppSettings.LearningPacks.GitHub is not configured.");

        var zipUrl = $"https://codeload.github.com/{gh.Owner}/{gh.Repo}/zip/refs/heads/{gh.Ref}";
        using var req = new HttpRequestMessage(HttpMethod.Get, zipUrl);

        if (!string.IsNullOrWhiteSpace(gh.Token))
        {
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", gh.Token);
        }

        using var resp = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        await using var zipStream = await resp.Content.ReadAsStreamAsync(ct);
        using var zip = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false);

        var rootPrefix = $"{gh.Repo}-{gh.Ref}/{gh.RootPath.Trim('/')}/";

        var packJsonEntries = zip.Entries
            .Where(e => !e.FullName.EndsWith("/", StringComparison.Ordinal))
            .Where(e => e.FullName.StartsWith(rootPrefix, StringComparison.OrdinalIgnoreCase))
            .Where(e => e.FullName.EndsWith("/pack.json", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (packJsonEntries.Length == 0)
        {
            return new LearningPackContentSnapshot(DateTimeOffset.UtcNow, Array.Empty<LearningPack>());
        }

        var packs = new List<LearningPack>(packJsonEntries.Length);

        foreach (var packEntry in packJsonEntries)
        {
            ct.ThrowIfCancellationRequested();

            var packFolderInZip = packEntry.FullName[..^"pack.json".Length];

            LearningPackManifest manifest;
            await using (var s = packEntry.Open())
            {
                manifest = await JsonSerializer.DeserializeAsync<LearningPackManifest>(s, JsonOptions, ct)
                           ?? throw new InvalidOperationException($"Unable to parse pack.json for {packEntry.FullName}.");
            }

            var assetBaseUrl = $"/learning/assets/{Uri.EscapeDataString(manifest.Id)}";

            var packPages = new List<LearningPackPage>(manifest.Pages.Count);

            foreach (var pageRef in manifest.Pages)
            {
                var pageZipPath = $"{packFolderInZip}{pageRef.File.Replace("\\", "/", StringComparison.Ordinal)}";
                var pageEntry = zip.GetEntry(pageZipPath);

                if (pageEntry is null)
                {
                    continue;
                }

                string markdown;
                await using (var ps = pageEntry.Open())
                using (var sr = new StreamReader(ps, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 4096, leaveOpen: false))
                {
                    markdown = await sr.ReadToEndAsync(ct);
                }

                packPages.Add(new LearningPackPage(
                    Id: pageRef.Id,
                    Title: pageRef.Title,
                    Markdown: markdown
                ));
            }

            var assets = LoadAssetsFromZip(zip, packFolderInZip, assetBaseUrl, ct);

            packs.Add(new LearningPack(manifest, packPages, assetBaseUrl, assets));
        }

        var ordered = packs
            .OrderBy(p => p.Manifest.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new LearningPackContentSnapshot(DateTimeOffset.UtcNow, ordered);
    }

    private static IReadOnlyList<LearningPackAsset> LoadAssetsFromZip(ZipArchive zip, string packFolderInZip, string assetBaseUrl, CancellationToken ct)
    {
        var assetsPrefix = $"{packFolderInZip}assets/";

        var entries = zip.Entries
            .Where(e => !e.FullName.EndsWith("/", StringComparison.Ordinal))
            .Where(e => e.FullName.StartsWith(assetsPrefix, StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (entries.Length == 0)
        {
            return Array.Empty<LearningPackAsset>();
        }

        var list = new List<LearningPackAsset>(entries.Length);

        foreach (var e in entries)
        {
            ct.ThrowIfCancellationRequested();

            var relative = e.FullName[assetsPrefix.Length..];
            var fileKey = ImageHelpers.NormaliseAssetKey(relative);

            using var s = e.Open();
            using var ms = new MemoryStream(capacity: (int)Math.Min(int.MaxValue, e.Length));
            s.CopyTo(ms);
            var bytes = ms.ToArray();

            list.Add(new LearningPackAsset(
                FileName: fileKey,
                ContentType: ImageHelpers.GuessContentType(fileKey),
                Bytes: bytes
            ));
        }

        return list
            .OrderBy(a => a.FileName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
