using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using BOTGC.API.Services.CompetitionProcessors;

using System;
using System.IO;
using System.Runtime;
using System.Threading.Tasks;
using BOTGC.API.Interfaces;

namespace BOTGC.API.Services
{
    public class FileCacheService : ICacheService
    {
        private readonly string _cacheDirectory;

        private readonly AppSettings _settings;
        private readonly ILogger<FileCacheService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public FileCacheService(IOptions<AppSettings> settings,
                                ILogger<FileCacheService> logger,
                                IHttpContextAccessor httpContextAccessor)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));

            _cacheDirectory = _settings.Cache.FileCacheStorage?.Path;

            if (!Directory.Exists(_cacheDirectory))
            {
                Directory.CreateDirectory(_cacheDirectory);
            }
        }

        private string GetFilePath(string key) => Path.Combine(_cacheDirectory, $"{key}.json");

        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            if (ShouldSkipCache())
            {
                _logger.LogInformation("Skipping cache retrieval due to 'Cache-Control: no-cache' header.");
                return null;
            }

            string filePath = GetFilePath(key);

            if (!File.Exists(filePath))
                return null;

            string json = await File.ReadAllTextAsync(filePath);
            var cachedItem = JsonConvert.DeserializeObject<CachedItem<T>>(json);

            if (cachedItem == null || cachedItem.Expiration < DateTime.UtcNow)
            {
                await RemoveAsync(key);
                return null;
            }

            return cachedItem.Value;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration) where T : class
        {
            string filePath = GetFilePath(key);
            var cachedItem = new CachedItem<T>
            {
                Value = value,
                Expiration = DateTime.UtcNow.Add(expiration)
            };

            string json = JsonConvert.SerializeObject(cachedItem, Formatting.Indented);
            await File.WriteAllTextAsync(filePath, json);
        }

        public async Task RemoveAsync(string key)
        {
            string filePath = GetFilePath(key);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
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
    }

    public class CachedItem<T>
    {
        public required T Value { get; set; } 
        public required DateTime Expiration { get; set; }
    }
}
