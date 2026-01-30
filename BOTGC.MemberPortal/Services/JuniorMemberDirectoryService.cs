using System.Net;
using System.Text.Json;
using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace BOTGC.MemberPortal.Services;

public sealed class JuniorMemberDirectoryService : IJuniorMemberDirectoryService
{
    private const string CacheKey = "admin:juniors:v2";
    private const string RefreshLockResource = "lock:admin:juniors:v2:refresh";

    private static readonly TimeSpan RefreshAfter = TimeSpan.FromMinutes(30);
    private static readonly TimeSpan LockExpiry = TimeSpan.FromSeconds(90);

    private readonly HttpClient _client;
    private readonly IDistributedCache _cache;
    private readonly JsonSerializerOptions _json;
    private readonly IDistributedLockManager _lockManager;
    private readonly ILogger<JuniorMemberDirectoryService> _logger;

    public JuniorMemberDirectoryService(
        IHttpClientFactory httpClientFactory,
        IDistributedCache cache,
        JsonSerializerOptions json,
        IDistributedLockManager lockManager,
        ILogger<JuniorMemberDirectoryService> logger)
    {
        _client = httpClientFactory.CreateClient("Api");
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _json = json ?? throw new ArgumentNullException(nameof(json));
        _lockManager = lockManager ?? throw new ArgumentNullException(nameof(lockManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<MemberSearchResult>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        query ??= string.Empty;
        query = query.Trim();

        if (query.Length < 2)
        {
            return Array.Empty<MemberSearchResult>();
        }

        var snapshot = await GetSnapshotAsync(cancellationToken);

        if (snapshot is null)
        {
            _ = TriggerRefreshInBackground();
            return Array.Empty<MemberSearchResult>();
        }

        if (DateTimeOffset.UtcNow - snapshot.FetchedAtUtc >= RefreshAfter)
        {
            _ = TriggerRefreshInBackground();
        }

        var results = snapshot.Members
            .Where(m =>
                (!string.IsNullOrWhiteSpace(m.FullName) && m.FullName.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(m.FirstName) && m.FirstName.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (!string.IsNullOrWhiteSpace(m.LastName) && m.LastName.Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (m.MemberNumber.HasValue && m.MemberNumber.Value.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)) ||
                (m.PlayerId.HasValue && m.PlayerId.Value.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(m => m.LastName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.FirstName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .Take(20)
            .Select(m => new MemberSearchResult
            {
                PlayerId = m.PlayerId,
                MemberNumber = m.MemberNumber,
                FullName = m.FullName ?? BuildFullName(m.FirstName, m.LastName),
                MembershipCategory = m.MembershipCategory,
                DateOfBirth = m.DateOfBirth, 
                AvatarDataUri = GetAvatar(m.MemberNumber!.Value)
            })
            .ToList();

        return results;
    }

    private string GetAvatar(int memberId)
    {
        // TODO work out where to get the avatars from
        return "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";
    }

    private Task TriggerRefreshInBackground()
    {
        return Task.Run(async () =>
        {
            try
            {
                await using var redLock = await _lockManager.AcquireLockAsync(
                    RefreshLockResource,
                    expiry: LockExpiry,
                    waitTime: TimeSpan.Zero,
                    retryTime: TimeSpan.Zero,
                    cancellationToken: CancellationToken.None);

                if (!redLock.IsAcquired)
                {
                    return;
                }

                await RefreshFromApiAndUpdateCacheAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Junior directory refresh failed.");
            }
        });
    }

    private async Task<JuniorDirectorySnapshot?> GetSnapshotAsync(CancellationToken cancellationToken)
    {
        var cached = await _cache.GetStringAsync(CacheKey, cancellationToken);
        if (string.IsNullOrWhiteSpace(cached))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<JuniorDirectorySnapshot>(cached, _json);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Junior directory cache payload was invalid JSON.");
            return null;
        }
    }

    private async Task RefreshFromApiAndUpdateCacheAsync(CancellationToken cancellationToken)
    {
        var response = await _client.GetAsync("api/members/juniors", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NoContent)
        {
            var empty = new JuniorDirectorySnapshot
            {
                FetchedAtUtc = DateTimeOffset.UtcNow,
                Members = new List<ApiMemberDto>()
            };

            await _cache.SetStringAsync(CacheKey, JsonSerializer.Serialize(empty, _json), cancellationToken);
            return;
        }

        response.EnsureSuccessStatusCode();

        var juniors = await response.Content.ReadFromJsonAsync<List<ApiMemberDto>>(_json, cancellationToken);
        var list = juniors ?? new List<ApiMemberDto>();

        var snapshot = new JuniorDirectorySnapshot
        {
            FetchedAtUtc = DateTimeOffset.UtcNow,
            Members = list
        };

        await _cache.SetStringAsync(CacheKey, JsonSerializer.Serialize(snapshot, _json), cancellationToken);
    }

    private static string BuildFullName(string? first, string? last)
    {
        var f = (first ?? string.Empty).Trim();
        var l = (last ?? string.Empty).Trim();

        if (f.Length == 0 && l.Length == 0)
        {
            return string.Empty;
        }

        if (f.Length == 0)
        {
            return l;
        }

        if (l.Length == 0)
        {
            return f;
        }

        return $"{f} {l}";
    }

    private sealed class JuniorDirectorySnapshot
    {
        public DateTimeOffset FetchedAtUtc { get; set; }

        public List<ApiMemberDto> Members { get; set; } = new();
    }

    private sealed class ApiMemberDto
    {
        public int? PlayerId { get; set; }

        public int? MemberNumber { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? FullName { get; set; }

        public string? MembershipCategory { get; set; }

        public DateTime? DateOfBirth { get; set; }
    }
}
