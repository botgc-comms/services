using System.Collections.Concurrent;
using System.Text.Json;
using BOTGC.API.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace BOTGC.API.Services;

public class RedisCacheService : ICacheService
{
    private const string WarmedKeysItemsKey = "__Cache_WarmedKeys";
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> KeyLocks = new(StringComparer.Ordinal);

    private readonly IDistributedCache _cache;
    private readonly ILogger<RedisCacheService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RedisCacheService(IDistributedCache cache,
                             ILogger<RedisCacheService> logger,
                             IHttpContextAccessor httpContextAccessor)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public async Task<T?> GetAsync<T>(string key, bool force = false) where T : class
    {
        T retVal = null;

        var gate = KeyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync().ConfigureAwait(false);

        try
        {
            if (!force && ShouldSkipCache() && !IsKeyWarmedForThisRequest(key))
            {
                _logger.LogInformation("Skipping cache GET for key '{Key}' due to 'Cache-Control: no-cache' (first request in this context).", key);
                return null;
            }

            var data = await _cache.GetStringAsync(key).ConfigureAwait(false);
            if (string.IsNullOrEmpty(data)) return null;

            retVal = JsonSerializer.Deserialize<T>(data);
        }
        catch (Exception ex)
        {
            retVal = default(T);
            _logger.LogError(ex, "Error retrieving key '{Key}' from cache.", key);      
        }
        finally
        {
            gate.Release();
        }

        return retVal;
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
    {
        var gate = KeyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            var json = JsonSerializer.Serialize(value, new JsonSerializerOptions { WriteIndented = true });
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            await _cache.SetStringAsync(key, json, options).ConfigureAwait(false);
            MarkKeyWarmedForThisRequest(key);
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task RemoveAsync(string key)
    {
        var gate = KeyLocks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync().ConfigureAwait(false);
        try
        {
            await _cache.RemoveAsync(key).ConfigureAwait(false);
            UnmarkKeyWarmedForThisRequest(key);
        }
        finally
        {
            gate.Release();
        }
    }

    private bool ShouldSkipCache()
    {
        var context = _httpContextAccessor.HttpContext;
        if (context?.Request.Headers.TryGetValue("Cache-Control", out var cacheControl) == true)
        {
            return cacheControl.ToString().Contains("no-cache", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    private bool IsKeyWarmedForThisRequest(string key)
    {
        var set = GetWarmedSetForThisRequest();
        return set != null && set.Contains(key);
    }

    private void MarkKeyWarmedForThisRequest(string key)
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx == null) return; // background context: no-op

        var set = GetOrCreateWarmedSetForThisRequest();
        set.Add(key);
    }

    private void UnmarkKeyWarmedForThisRequest(string key)
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx == null) return; // background context: no-op

        var set = GetWarmedSetForThisRequest();
        set?.Remove(key);
    }

    private HashSet<string>? GetWarmedSetForThisRequest()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx == null) return null;

        if (ctx.Items.TryGetValue(WarmedKeysItemsKey, out var obj) && obj is HashSet<string> set)
        {
            return set;
        }

        return null;
    }

    private HashSet<string> GetOrCreateWarmedSetForThisRequest()
    {
        var ctx = _httpContextAccessor.HttpContext;
        if (ctx == null)
        {
            // In non-HTTP execution (background services), we do not track per-request warmed keys.
            // Return a throwaway set to satisfy callers without throwing or leaking state.
            return new HashSet<string>(StringComparer.Ordinal);
        }

        if (ctx.Items.TryGetValue(WarmedKeysItemsKey, out var obj) && obj is HashSet<string> existing)
        {
            return existing;
        }

        var newSet = new HashSet<string>(StringComparer.Ordinal);
        ctx.Items[WarmedKeysItemsKey] = newSet;
        return newSet;
    }
}
