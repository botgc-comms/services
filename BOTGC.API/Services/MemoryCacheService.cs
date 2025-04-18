using BOTGC.API.Interfaces;
using Microsoft.Extensions.Caching.Memory;

namespace BOTGC.API.Services
{
    public class MemoryCacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly IHttpContextAccessor _httpContextAccessor;

        private readonly ILogger<MemoryCacheService> _logger;

        public MemoryCacheService(IMemoryCache cache, ILogger<MemoryCacheService> logger, IHttpContextAccessor httpContextAccessor)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
        }

        public Task<T?> GetAsync<T>(string key) where T : class
        {
            if (ShouldSkipCache())
            {
                _logger.LogInformation("Skipping cache retrieval due to 'Cache-Control: no-cache' header.");
                return Task.FromResult<T?>(null);
            }

            if (_cache.TryGetValue(key, out T value))
            {
                _logger.LogInformation("Cache hit for key: {Key}", key);
                return Task.FromResult(value);
            }

            _logger.LogInformation("Cache miss for key: {Key}", key);
            return Task.FromResult(default(T));
        }

        public Task SetAsync<T>(string key, T value, TimeSpan expiration) where T: class
        {
            if (ShouldSkipCache())
            {
                _logger.LogInformation("Skipping cache storage due to 'Cache-Control: no-cache' header.");
                return Task.CompletedTask;
            }

            _logger.LogInformation("Caching key: {Key} for {Duration} seconds", key, expiration.TotalSeconds);
            _cache.Set(key, value, expiration);
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key)
        {
            _logger.LogInformation("Removing key from cache: {Key}", key);
            _cache.Remove(key);
            return Task.CompletedTask;
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
    }

}
