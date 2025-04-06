using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Services.Interfaces;
using System;
using System.Threading.Tasks;

namespace BOTGC.API.Services
{
    public class RedisCacheService : ICacheService
    {
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

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            if (ShouldSkipCache())
            {
                _logger.LogInformation("Skipping cache retrieval due to 'Cache-Control: no-cache' header.");
                return null;
            }

            var data = await _cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(data))
                return null;

            return JsonConvert.DeserializeObject<T>(data);
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
        {
            var json = JsonConvert.SerializeObject(value, Formatting.Indented);
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration
            };

            await _cache.SetStringAsync(key, json, options);
        }

        public async Task RemoveAsync(string key)
        {
            await _cache.RemoveAsync(key);
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
