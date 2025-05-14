using BOTGC.MembershipApplication.Interfaces;
using BOTGC.MembershipApplication.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BOTGC.MembershipApplication.Services
{
    public class MembershipCategoryCache : IMembershipCategoryCache
    {
        private List<MembershipCategoryGroup> _categories = new();
        private readonly object _lock = new();
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<MembershipCategoryCache> _logger;

        public MembershipCategoryCache(IHttpClientFactory httpClientFactory, ILogger<MembershipCategoryCache> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<IReadOnlyList<MembershipCategoryGroup>> GetAll()
        {
            if (!_categories.Any()) await RefreshAsync();

            lock (_lock)
            {
                return _categories.ToList();
            }
        }

        public void Update(IEnumerable<MembershipCategoryGroup> categories)
        {
            lock (_lock)
            {
                _categories = categories.ToList();
            }
        }

        public async Task RefreshAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var client = _httpClientFactory.CreateClient("MembershipApi");
                var response = await client.GetAsync("api/members/categories", cancellationToken);
                response.EnsureSuccessStatusCode();

                var data = await response.Content.ReadFromJsonAsync<List<MembershipCategoryGroup>>(cancellationToken: cancellationToken);

                if (data != null && data.Count > 0)
                {
                    Update(data);
                    _logger.LogInformation("MembershipCategoryCache: Successfully refreshed with {Count} categories.", data.Count);
                }
                else
                {
                    _logger.LogWarning("MembershipCategoryCache: Received empty or null category list.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MembershipCategoryCache: Failed to refresh membership categories.");
            }
        }
    }
}
