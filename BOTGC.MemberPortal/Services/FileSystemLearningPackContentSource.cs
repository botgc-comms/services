// LearningPackServices.cs
using System.Buffers;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BOTGC.MemberPortal.Common;

namespace BOTGC.MemberPortal.Services;

public sealed class FileSystemLearningPackContentSource : ILearningPackContentSource
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly AppSettings _settings;

    public FileSystemLearningPackContentSource(AppSettings settings)
    {
        _settings = settings;
    }

    public async Task<LearningPackContentSnapshot> LoadAsync(CancellationToken ct = default)
    {
        var fs = _settings.LearningPacks.FileSystem ?? throw new InvalidOperationException("AppSettings.LearningPacks.FileSystem is not configured.");

        if (!Directory.Exists(fs.RootPath))
        {
            return new LearningPackContentSnapshot(DateTimeOffset.UtcNow, Array.Empty<LearningPack>());
        }

        var packFolders = Directory.EnumerateDirectories(fs.RootPath).ToArray();
        if (packFolders.Length == 0)
        {
            return new LearningPackContentSnapshot(DateTimeOffset.UtcNow, Array.Empty<LearningPack>());
        }

        var packs = new List<LearningPack>(packFolders.Length);

        foreach (var packFolder in packFolders)
        {
            ct.ThrowIfCancellationRequested();

            var packJsonPath = Path.Combine(packFolder, "pack.json");
            if (!File.Exists(packJsonPath))
            {
                continue;
            }

            LearningPackManifest manifest;
            await using (var s = File.OpenRead(packJsonPath))
            {
                manifest = await JsonSerializer.DeserializeAsync<LearningPackManifest>(s, JsonOptions, ct)
                           ?? throw new InvalidOperationException($"Unable to parse {packJsonPath}.");
            }

            var assetBaseUrl = $"/learning/assets/{Uri.EscapeDataString(manifest.Id)}";

            var packPages = new List<LearningPackPage>(manifest.Pages.Count);

            foreach (var pageRef in manifest.Pages)
            {
                var pagePath = Path.Combine(packFolder, pageRef.File.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(pagePath))
                {
                    continue;
                }

                var markdown = await File.ReadAllTextAsync(pagePath, Encoding.UTF8, ct);

                packPages.Add(new LearningPackPage(
                    Id: pageRef.Id,
                    Title: pageRef.Title,
                    Markdown: markdown
                ));
            }

            var assets = await LoadAssetsFromFileSystemAsync(packFolder, ct);

            packs.Add(new LearningPack(manifest, packPages, assetBaseUrl, assets));
        }

        var ordered = packs
            .OrderBy(p => p.Manifest.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new LearningPackContentSnapshot(DateTimeOffset.UtcNow, ordered);
    }

    private static async Task<IReadOnlyList<LearningPackAsset>> LoadAssetsFromFileSystemAsync(string packFolder, CancellationToken ct)
    {
        var assetsDir = Path.Combine(packFolder, "assets");
        if (!Directory.Exists(assetsDir))
        {
            return Array.Empty<LearningPackAsset>();
        }

        var files = Directory.EnumerateFiles(assetsDir, "*", SearchOption.AllDirectories).ToArray();
        if (files.Length == 0)
        {
            return Array.Empty<LearningPackAsset>();
        }

        var list = new List<LearningPackAsset>(files.Length);

        foreach (var filePath in files)
        {
            ct.ThrowIfCancellationRequested();

            var relative = Path.GetRelativePath(assetsDir, filePath);
            var fileKey = ImageHelpers.NormaliseAssetKey(relative);

            var bytes = await File.ReadAllBytesAsync(filePath, ct);

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
