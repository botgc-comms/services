using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services
{
    public sealed class EposProductSeedService : IHostedService
    {
        private readonly IEposStore _store;
        private readonly AppSettings _settings;
        private readonly ILogger<EposProductSeedService> _logger;

        public EposProductSeedService(
            IEposStore store,
            IOptions<AppSettings> settings,
            ILogger<EposProductSeedService> logger)
        {
            _store = store ?? throw new ArgumentNullException(nameof(store));
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var configuredProducts = _settings.EposBenefits?.Products;

            if (configuredProducts == null || configuredProducts.Count == 0)
            {
                _logger.LogInformation("EposProductSeedService: no configured EPOS products to seed.");
                return;
            }

            foreach (var cfg in configuredProducts)
            {
                if (!Guid.TryParse(cfg.ProductId, out var productId))
                {
                    _logger.LogWarning(
                        "EposProductSeedService: invalid ProductId '{ProductId}' for product code {Code}. Skipping.",
                        cfg.ProductId,
                        cfg.Code);
                    continue;
                }

                var allowedCategories = cfg.AllowedMembershipCategories != null &&
                                        cfg.AllowedMembershipCategories.Count > 0
                    ? string.Join(";", cfg.AllowedMembershipCategories.Where(c => !string.IsNullOrWhiteSpace(c)))
                    : null;

                var entity = new EposProductEntity
                {
                    PartitionKey = "Product",
                    RowKey = EposProductEntity.RowKeyFor(productId),
                    Code = cfg.Code ?? string.Empty,
                    DisplayName = cfg.DisplayName ?? string.Empty,
                    Description = cfg.Description,
                    Category = cfg.Category ?? string.Empty,
                    DefaultRedemptionValue = cfg.DefaultRedemptionValue,
                    DefaultAllowanceCharge = cfg.DefaultAllowanceCharge,
                    IsActive = cfg.IsActive,
                    ProductExpiresAtUtc = cfg.ProductExpiresAtUtc,
                    AllowedMembershipCategories = allowedCategories
                };

                await _store.UpsertProductAsync(entity, cancellationToken);
            }

            _logger.LogInformation(
                "EposProductSeedService: ensured {Count} configured EPOS products are present.",
                configuredProducts.Count);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
