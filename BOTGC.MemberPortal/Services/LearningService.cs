// LearningPackServices.cs
using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Azure;
using Azure.Data.Tables;
using BOTGC.MemberPortal.Common;
using BOTGC.MemberPortal.Interfaces;
using Markdig;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Html;

namespace BOTGC.MemberPortal.Services;

public sealed record LearningPackPageRef(string Id, string Title, string Summary, string File, string? Type = null);


public sealed record LearningPackManifest(
    string Id,
    string Title,
    string Summary,
    string Description,
    int EstimatedMinutes,
    IReadOnlyList<string> MandatoryFor,
    int? Priority,
    int Version,
    IReadOnlyList<LearningPackPageRef> Pages
);

public sealed record LearningPackAsset(
    string FileName,
    string ContentType,
    byte[] Bytes
);

public sealed record LearningPack(
    LearningPackManifest Manifest,
    IReadOnlyList<LearningPackPage> Pages,
    string AssetBaseUrl,
    IReadOnlyList<LearningPackAsset> Assets

);
public sealed record LearningPackPage(
    string Id,
    string Title,
    string Markdown
);

public sealed record LearningPackSummary(
    string Id,
    string Title,
    string Summary,
    int EstimatedMinutes,
    int? Priority,
    int Version,
    IReadOnlyList<string> MandatoryFor
);

public sealed record LearningPackContentSnapshot(
    DateTimeOffset LoadedAtUtc,
    IReadOnlyList<LearningPack> Packs
);

public sealed record UserPackProgress(
    string UserId,
    string PackId,
    DateTimeOffset? FirstViewedAtUtc,
    DateTimeOffset? LastViewedAtUtc,
    int PagesViewedCount,
    bool IsCompleted,
    DateTimeOffset? CompletedAtUtc,
    string? CompletedContentVersion,
    IReadOnlyList<string> ReadPageIds
);


public sealed record PackCatalogue(
    string ContentVersion,
    IReadOnlyList<LearningPackSummary> Packs
);

public interface ILearningPackContentSource
{
    Task<LearningPackContentSnapshot> LoadAsync(CancellationToken ct = default);
}

public interface ILearningPackProvider
{
    Task<LearningPackContentSnapshot?> GetSnapshotAsync(CancellationToken ct = default);
}

public interface ILearningPackProgressRepository
{
    Task<UserPackProgress?> GetAsync(string userId, string packId, CancellationToken ct = default);

    Task<IReadOnlyList<UserPackProgress>> ListAsync(string userId, CancellationToken ct = default);

    Task RecordPackViewedAsync(string userId, string packId, DateTimeOffset viewedAtUtc, CancellationToken ct = default);

    Task RecordPageViewedAsync(string userId, string packId, string pageId, DateTimeOffset viewedAtUtc, CancellationToken ct = default);

    Task MarkCompletedAsync(string userId, string packId, DateTimeOffset completedAtUtc, string contentVersion, CancellationToken ct = default);
}

public sealed class LearningPackContentCache
{
    private readonly ICacheService _cache;
    private readonly AppSettings _settings;

    public LearningPackContentCache(ICacheService cache, AppSettings settings)
    {
        _cache = cache;
        _settings = settings;
    }

    public string PrimaryKey => $"{_settings.LearningPacks.CacheKeyPrefix}:data:primary";
    public string StandbyKey => $"{_settings.LearningPacks.CacheKeyPrefix}:data:standby";

    public Task<LearningPackContentSnapshot?> GetPrimaryAsync(CancellationToken ct = default)
    {
        return _cache.GetAsync<LearningPackContentSnapshot>(PrimaryKey, ct);
    }

    public Task<LearningPackContentSnapshot?> GetStandbyAsync(CancellationToken ct = default)
    {
        return _cache.GetAsync<LearningPackContentSnapshot>(StandbyKey, ct);
    }

    public Task SetPrimaryAsync(LearningPackContentSnapshot snapshot, CancellationToken ct = default)
    {
        return _cache.SetAsync(PrimaryKey, snapshot, _settings.LearningPacks.PrimaryCacheTtl, ct);
    }

    public Task SetStandbyAsync(LearningPackContentSnapshot snapshot, CancellationToken ct = default)
    {
        return _cache.SetAsync(StandbyKey, snapshot, _settings.LearningPacks.StandbyCacheTtl, ct);
    }
}

public sealed class LearningPackContentSynchroniser
{
    private readonly ILearningPackContentSource _source;
    private readonly LearningPackContentCache _cache;
    private readonly LearningPackCataloguePublisher _publisher;
    private readonly SemaphoreSlim _refreshLock = new SemaphoreSlim(1, 1);

    public LearningPackContentSynchroniser(ILearningPackContentSource source, 
        LearningPackContentCache cache,
        LearningPackCataloguePublisher publisher)
    {
        _source = source;
        _cache = cache;
        _publisher = publisher;
    }

    public async Task<LearningPackContentSnapshot> RefreshAsync(CancellationToken ct = default)
    {
        await _refreshLock.WaitAsync(ct);
        try
        {
            var snapshot = await _source.LoadAsync(ct);

            if (snapshot.Packs.Count == 0)
            {
                return snapshot;
            }

            await _cache.SetPrimaryAsync(snapshot, ct);
            await _cache.SetStandbyAsync(snapshot, ct);
            await _publisher.PublishAsync(snapshot, ct);

            return snapshot;
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}


public sealed class CachedLearningPackProvider : ILearningPackProvider
{
    private readonly LearningPackContentCache _cache;
    private readonly LearningPackContentSynchroniser _sync;
    private readonly ILogger<CachedLearningPackProvider> _logger;

    public CachedLearningPackProvider(
        LearningPackContentCache cache,
        LearningPackContentSynchroniser sync,
        ILogger<CachedLearningPackProvider> logger)
    {
        _cache = cache;
        _sync = sync;
        _logger = logger;
    }

    public async Task<LearningPackContentSnapshot?> GetSnapshotAsync(CancellationToken ct = default)
    {
        var primary = await _cache.GetPrimaryAsync(ct);
        if (primary is not null)
        {
            return primary;
        }

        var standby = await _cache.GetStandbyAsync(ct);
        if (standby is not null)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _sync.RefreshAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Learning packs refresh failed (standby served).");
                }
            });

            return standby;
        }

        var loaded = await _sync.RefreshAsync(ct);
        if (loaded.Packs.Count > 0)
        {
            return loaded;
        }

        return null;
    }
}


public sealed class LearningPackContentWarmupHostedService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LearningPackContentWarmupHostedService> _logger;

    public LearningPackContentWarmupHostedService(
        IServiceScopeFactory scopeFactory,
        ILogger<LearningPackContentWarmupHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();

        var cache = scope.ServiceProvider.GetRequiredService<LearningPackContentCache>();
        var sync = scope.ServiceProvider.GetRequiredService<LearningPackContentSynchroniser>();

        var primary = await cache.GetPrimaryAsync(cancellationToken);
        if (primary is not null && primary.Packs.Count > 0)
        {
            _logger.LogInformation("Learning packs warmup: primary present ({Count} packs).", primary.Packs.Count);
            return;
        }

        var standby = await cache.GetStandbyAsync(cancellationToken);
        if (standby is not null && standby.Packs.Count > 0)
        {
            _logger.LogInformation("Learning packs warmup: primary missing, standby present ({Count} packs). Background refresh will repopulate primary.", standby.Packs.Count);

            _ = Task.Run(async () =>
            {
                try
                {
                    using var refreshScope = _scopeFactory.CreateScope();
                    var refresher = refreshScope.ServiceProvider.GetRequiredService<LearningPackContentSynchroniser>();
                    await refresher.RefreshAsync(CancellationToken.None);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Learning packs warmup: background refresh failed.");
                }
            });

            return;
        }

        var loaded = await sync.RefreshAsync(cancellationToken);

        if (loaded.Packs.Count == 0)
        {
            _logger.LogWarning("Learning packs warmup: loaded from source but zero packs were found.");
            return;
        }

        _logger.LogInformation("Learning packs warmup: loaded from source ({Count} packs).", loaded.Packs.Count);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

public sealed class LearningPackService
{
    private readonly ILearningPackProvider _provider;
    private readonly ILearningPackProgressRepository _progress;

    public LearningPackService(ILearningPackProvider provider, ILearningPackProgressRepository progress)
    {
        _provider = provider;
        _progress = progress;
    }

    public async Task<PackCatalogue> ListAvailableAsync(CancellationToken ct = default)
    {
        var snapshot = await _provider.GetSnapshotAsync(ct) ?? new LearningPackContentSnapshot(DateTimeOffset.UtcNow, Array.Empty<LearningPack>());

        var contentVersion = ComputeContentVersion(snapshot);

        var list = snapshot.Packs
            .Select(p => new LearningPackSummary(
                Id: p.Manifest.Id,
                Title: p.Manifest.Title,
                Summary: p.Manifest.Summary,
                EstimatedMinutes: p.Manifest.EstimatedMinutes,
                Priority: p.Manifest.Priority,
                Version: p.Manifest.Version,
                MandatoryFor: p.Manifest.MandatoryFor
            ))
            .OrderBy(x => x.Title, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new PackCatalogue(contentVersion, list);
    }

    public async Task<LearningPack?> GetPackAsync(string packId, CancellationToken ct = default)
    {
        var snapshot = await _provider.GetSnapshotAsync(ct);
        if (snapshot is null)
        {
            return null;
        }

        return snapshot.Packs.FirstOrDefault(p => string.Equals(p.Manifest.Id, packId, StringComparison.OrdinalIgnoreCase));
    }

    public Task<IReadOnlyList<UserPackProgress>> ListUserProgressAsync(string userId, CancellationToken ct = default)
    {
        return _progress.ListAsync(userId, ct);
    }

    public Task<UserPackProgress?> GetUserProgressAsync(string userId, string packId, CancellationToken ct = default)
    {
        return _progress.GetAsync(userId, packId, ct);
    }

    public Task RecordPackViewedAsync(string userId, string packId, CancellationToken ct = default)
    {
        return _progress.RecordPackViewedAsync(userId, packId, DateTimeOffset.UtcNow, ct);
    }

    public Task RecordPageViewedAsync(string userId, string packId, string pageId, CancellationToken ct = default)
    {
        return _progress.RecordPageViewedAsync(userId, packId, pageId, DateTimeOffset.UtcNow, ct);
    }

    public async Task<bool> MarkPackCompletedAsync(string userId, string packId, CancellationToken ct = default)
    {
        var snapshot = await _provider.GetSnapshotAsync(ct);
        if (snapshot is null)
        {
            return false;
        }

        var pack = snapshot.Packs.FirstOrDefault(p => string.Equals(p.Manifest.Id, packId, StringComparison.OrdinalIgnoreCase));
        if (pack is null)
        {
            return false;
        }

        var contentVersion = ComputeContentVersion(snapshot);

        await _progress.MarkCompletedAsync(userId, pack.Manifest.Id, DateTimeOffset.UtcNow, contentVersion, ct);

        return true;
    }

    public static string ComputeContentVersion(LearningPackContentSnapshot snapshot)
    {
        var ids = snapshot.Packs
            .Select(p => $"{p.Manifest.Id}:{p.Manifest.Version}")
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();

        var payload = string.Join(",", ids);
        var bytes = Encoding.UTF8.GetBytes(payload);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

}


public sealed class TableStorageLearningPackProgressRepository : ILearningPackProgressRepository
{
    private readonly TableClient _progress;
    private readonly TableClient _pageViews;

    public TableStorageLearningPackProgressRepository(AppSettings settings)
    {
        var ts = settings.LearningPacks.TableStorage ?? throw new InvalidOperationException("AppSettings.LearningPacks.TableStorage is not configured.");

        var service = new TableServiceClient(ts.ConnectionString);

        _progress = service.GetTableClient(ts.ProgressTableName);
        _pageViews = service.GetTableClient(ts.PageViewsTableName);

        _progress.CreateIfNotExists();
        _pageViews.CreateIfNotExists();
    }

    public async Task<UserPackProgress?> GetAsync(string userId, string packId, CancellationToken ct = default)
    {
        var rowKey = PackRowKey(packId);

        try
        {
            var e = (await _progress.GetEntityAsync<ProgressEntity>(userId, rowKey, cancellationToken: ct)).Value;
            return e.ToModel();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }

    public async Task<IReadOnlyList<UserPackProgress>> ListAsync(string userId, CancellationToken ct = default)
    {
        var list = new List<UserPackProgress>(64);

        var filter = $"PartitionKey eq '{Escape(userId)}'";
        await foreach (var e in _progress.QueryAsync<ProgressEntity>(filter: filter, maxPerPage: 500, cancellationToken: ct))
        {
            list.Add(e.ToModel());
        }

        return list
            .OrderByDescending(x => x.LastViewedAtUtc ?? DateTimeOffset.MinValue)
            .ThenBy(x => x.PackId, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public Task RecordPackViewedAsync(string userId, string packId, DateTimeOffset viewedAtUtc, CancellationToken ct = default)
    {
        return UpsertProgressAsync(userId, packId, viewedAtUtc, pageId: null, ct);
    }

    public async Task RecordPageViewedAsync(string userId, string packId, string pageId, DateTimeOffset viewedAtUtc, CancellationToken ct = default)
    {
        var pv = new PageViewEntity
        {
            PartitionKey = $"{userId}|{packId}",
            RowKey = $"{viewedAtUtc:yyyyMMddHHmmssfff}_{SafeRowKeyPart(pageId)}",
            UserId = userId,
            PackId = packId,
            PageId = pageId,
            ViewedAtUtc = viewedAtUtc
        };

        await _pageViews.UpsertEntityAsync(pv, TableUpdateMode.Replace, ct);

        await UpsertProgressAsync(userId, packId, viewedAtUtc, pageId, ct);
    }

    public async Task MarkCompletedAsync(string userId, string packId, DateTimeOffset completedAtUtc, string contentVersion, CancellationToken ct = default)
    {
        var rowKey = PackRowKey(packId);

        var res = await _progress.GetEntityIfExistsAsync<ProgressEntity>(userId, rowKey, cancellationToken: ct);

        var existing = res.HasValue
            ? res.Value
            : new ProgressEntity
            {
                PartitionKey = userId,
                RowKey = rowKey,
                UserId = userId,
                PackId = packId,
                FirstViewedAtUtc = null,
                LastViewedAtUtc = null,
                PagesViewedCount = 0,
                IsCompleted = false,
                CompletedAtUtc = null,
                CompletedContentVersion = null,
                ReadPageIdsCsv = null
            };

        var updated = new ProgressEntity
        {
            PartitionKey = userId,
            RowKey = rowKey,
            UserId = userId,
            PackId = packId,
            FirstViewedAtUtc = existing.FirstViewedAtUtc,
            LastViewedAtUtc = existing.LastViewedAtUtc ?? completedAtUtc,
            PagesViewedCount = existing.PagesViewedCount,
            IsCompleted = true,
            CompletedAtUtc = completedAtUtc,
            CompletedContentVersion = contentVersion,
            ReadPageIdsCsv = existing.ReadPageIdsCsv,
            ETag = existing.ETag
        };

        if (!res.HasValue)
        {
            await _progress.UpsertEntityAsync(updated, TableUpdateMode.Replace, ct);
            return;
        }

        await _progress.UpdateEntityAsync(updated, updated.ETag, TableUpdateMode.Replace, ct);

    }

    private async Task UpsertProgressAsync(string userId, string packId, DateTimeOffset viewedAtUtc, string? pageId, CancellationToken ct)
    {
        var rowKey = PackRowKey(packId);

        var res = await _progress.GetEntityIfExistsAsync<ProgressEntity>(userId, rowKey, cancellationToken: ct);

        var existing = res.HasValue
            ? res.Value
            : new ProgressEntity
            {
                PartitionKey = userId,
                RowKey = rowKey,
                UserId = userId,
                PackId = packId,
                FirstViewedAtUtc = null,
                LastViewedAtUtc = null,
                PagesViewedCount = 0,
                IsCompleted = false,
                CompletedAtUtc = null,
                CompletedContentVersion = null,
                ReadPageIdsCsv = null
            };

        var first = existing.FirstViewedAtUtc ?? viewedAtUtc;
        var last = existing.LastViewedAtUtc is null ? viewedAtUtc : (viewedAtUtc > existing.LastViewedAtUtc.Value ? viewedAtUtc : existing.LastViewedAtUtc.Value);

        var readPageIds = ParseCsv(existing.ReadPageIdsCsv);

        if (!string.IsNullOrWhiteSpace(pageId))
        {
            readPageIds.Add(pageId);
        }

        var updated = new ProgressEntity
        {
            PartitionKey = userId,
            RowKey = rowKey,
            UserId = userId,
            PackId = packId,
            FirstViewedAtUtc = first,
            LastViewedAtUtc = last,
            PagesViewedCount = readPageIds.Count,
            IsCompleted = existing.IsCompleted,
            CompletedAtUtc = existing.CompletedAtUtc,
            CompletedContentVersion = existing.CompletedContentVersion,
            ReadPageIdsCsv = ToCsv(readPageIds),
            ETag = existing.ETag
        };

        if (!res.HasValue || existing.Timestamp is null)
        {
            await _progress.UpsertEntityAsync(updated, TableUpdateMode.Replace, ct);
            return;
        }

        await _progress.UpdateEntityAsync(updated, updated.ETag, TableUpdateMode.Replace, ct);
    }

    private static HashSet<string> ParseCsv(string? csv)
    {
        var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(csv))
        {
            return set;
        }

        foreach (var part in csv.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            set.Add(part);
        }

        return set;
    }

    private static string SafeRowKeyPart(string value)
    {
        value ??= string.Empty;
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }


    private static string? ToCsv(HashSet<string> set)
    {
        if (set.Count == 0)
        {
            return null;
        }

        return string.Join("|", set.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
    }

    private static string PackRowKey(string packId)
    {
        return $"PACK_{packId.ToLowerInvariant()}";
    }

    private static string Escape(string value)
    {
        return value.Replace("'", "''", StringComparison.Ordinal);
    }

    private sealed class ProgressEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string UserId { get; set; } = string.Empty;
        public string PackId { get; set; } = string.Empty;

        public DateTimeOffset? FirstViewedAtUtc { get; set; }
        public DateTimeOffset? LastViewedAtUtc { get; set; }
        public int PagesViewedCount { get; set; }

        public bool IsCompleted { get; set; }
        public DateTimeOffset? CompletedAtUtc { get; set; }
        public string? CompletedContentVersion { get; set; }

        public string? ReadPageIdsCsv { get; set; }

        public UserPackProgress ToModel()
        {
            var read = string.IsNullOrWhiteSpace(ReadPageIdsCsv)
                ? Array.Empty<string>()
                : ReadPageIdsCsv.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            var uniqueReadCount = read.Distinct(StringComparer.OrdinalIgnoreCase).Count();

            return new UserPackProgress(
                UserId: UserId,
                PackId: PackId,
                FirstViewedAtUtc: FirstViewedAtUtc,
                LastViewedAtUtc: LastViewedAtUtc,
                PagesViewedCount: uniqueReadCount,
                IsCompleted: IsCompleted,
                CompletedAtUtc: CompletedAtUtc,
                CompletedContentVersion: CompletedContentVersion,
                ReadPageIds: read
            );
        }
    }

    private sealed class PageViewEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string UserId { get; set; } = string.Empty;
        public string PackId { get; set; } = string.Empty;
        public string PageId { get; set; } = string.Empty;
        public DateTimeOffset ViewedAtUtc { get; set; }
    }
}

public sealed class LearningPackMarkdownAssetResolver
{
    public MarkdownDocument Resolve(MarkdownDocument doc, string assetBaseUrl)
    {
        assetBaseUrl ??= string.Empty;

        foreach (var link in doc.Descendants<LinkInline>())
        {
            if (!link.IsImage)
            {
                continue;
            }

            var url = link.Url;
            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            if (url.StartsWith("../assets/", StringComparison.OrdinalIgnoreCase))
            {
                var relative = url.Substring("../assets/".Length);
                link.Url = $"{assetBaseUrl}/{ImageHelpers.EncodePath(relative)}";
            }
        }

        return doc;
    }
}

public sealed class LearningMarkdownRenderer
{
    private readonly MarkdownPipeline _pipeline;
    private readonly LearningPackMarkdownAssetResolver _assetResolver;

    public LearningMarkdownRenderer(LearningPackMarkdownAssetResolver assetResolver)
    {
        _assetResolver = assetResolver;

        _pipeline = new MarkdownPipelineBuilder()
            .DisableHtml()
            .UseAdvancedExtensions()
            .Build();
    }

    public IHtmlContent ToHtml(string markdown, string assetBaseUrl)
    {
        var doc = Markdig.Markdown.Parse(markdown ?? string.Empty, _pipeline);

        _assetResolver.Resolve(doc, assetBaseUrl);

        var html = Markdig.Markdown.ToHtml(doc, _pipeline);
        return new HtmlString(html);
    }
}

public sealed class LearningPackCataloguePublisher
{
    private const string MetaPartition = "META";
    private const string MetaRow = "CURRENT";
    private const string PacksPartition = "PACKS";

    private readonly TableClient _catalogue;

    public LearningPackCataloguePublisher(AppSettings settings)
    {
        var ts = settings.LearningPacks.TableStorage ?? throw new InvalidOperationException("AppSettings.LearningPacks.TableStorage is not configured.");

        var service = new TableServiceClient(ts.ConnectionString);
        _catalogue = service.GetTableClient(ts.CatalogueTableName);

        _catalogue.CreateIfNotExists();
    }

    public async Task PublishAsync(LearningPackContentSnapshot snapshot, CancellationToken ct = default)
    {
        var contentVersion = LearningPackService.ComputeContentVersion(snapshot);

        var meta = new CatalogueMetaEntity
        {
            PartitionKey = MetaPartition,
            RowKey = MetaRow,
            ContentVersion = contentVersion,
            PublishedAtUtc = DateTimeOffset.UtcNow
        };

        await _catalogue.UpsertEntityAsync(meta, TableUpdateMode.Replace, ct);

        var actions = new List<TableTransactionAction>(100);

        foreach (var pack in snapshot.Packs)
        {
            var e = new CataloguePackEntity
            {
                PartitionKey = PacksPartition,
                RowKey = PackRowKey(pack.Manifest.Id),
                PackId = pack.Manifest.Id,
                Title = pack.Manifest.Title,
                Summary = pack.Manifest.Summary,
                EstimatedMinutes = pack.Manifest.EstimatedMinutes,
                Priority = pack.Manifest.Priority,
                Version = pack.Manifest.Version,
                MandatoryForCsv = ToCsv(pack.Manifest.MandatoryFor),
                ContentVersion = contentVersion
            };

            actions.Add(new TableTransactionAction(TableTransactionActionType.UpsertReplace, e));

            if (actions.Count == 100)
            {
                await _catalogue.SubmitTransactionAsync(actions, ct);
                actions.Clear();
            }
        }

        if (actions.Count > 0)
        {
            await _catalogue.SubmitTransactionAsync(actions, ct);
        }
    }

    private static string PackRowKey(string packId)
    {
        return $"PACK_{packId.ToLowerInvariant()}";
    }

    private static string? ToCsv(IReadOnlyList<string>? values)
    {
        if (values == null || values.Count == 0)
        {
            return null;
        }

        var filtered = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return filtered.Length == 0 ? null : string.Join("|", filtered);
    }

    private sealed class CatalogueMetaEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string ContentVersion { get; set; } = string.Empty;
        public DateTimeOffset PublishedAtUtc { get; set; }
    }

    private sealed class CataloguePackEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty;
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string PackId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public int EstimatedMinutes { get; set; }
        public int? Priority { get; set; }
        public int Version { get; set; }
        public string? MandatoryForCsv { get; set; }
        public string ContentVersion { get; set; } = string.Empty;
    }
}
