using BOTGC.MembershipApplication.Interfaces;
using BOTGC.MembershipApplication.Models;
using Microsoft.Extensions.Options;

namespace BOTGC.MembershipApplication.Services.Background
{
    public class MembershipCategoryPollingService : BackgroundService
    {
        private readonly IMembershipCategoryCache _cache;
        private readonly ILogger<MembershipCategoryPollingService> _logger;
        private readonly TimeSpan _interval = TimeSpan.FromMinutes(10);

        public MembershipCategoryPollingService(
            IMembershipCategoryCache cache,
            ILogger<MembershipCategoryPollingService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Polling latest membership categories from API...");
                    await _cache.RefreshAsync(stoppingToken);

                    var categories = await _cache.GetAll();
                    var count = categories.Count;

                    _logger.LogInformation("Membership category cache contains {Count} items.", count);

                    if (count == 0)
                    {
                        _logger.LogError("MembershipCategoryPollingService: Cache is still empty after refresh. Investigate API response.");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during membership category polling.");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}
