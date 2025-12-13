using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class EPOSGetAccountEntitlementsQueryHandler
    : QueryHandlerBase<GetAccountEntitlementsQuery, AccountEntitlementsDto?>
{
    private readonly AppSettings _settings;
    private readonly IEposStore _store;
    private readonly IMediator _mediator;
    private readonly IBenefitsQrTokenService _qrTokenService;

    public EPOSGetAccountEntitlementsQueryHandler(
        IOptions<AppSettings> settings,
        IEposStore store,
        IMediator mediator)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    public override async Task<AccountEntitlementsDto?> Handle(GetAccountEntitlementsQuery request, CancellationToken cancellationToken)
    {
        var account = await _store.GetAccountAsync(request.MemberId, cancellationToken);
        if (account is null)
        {
            return null;
        }

        var memberQuery = new GetMemberQuery { MemberNumber = request.MemberId };
        var member = await _mediator.Send(memberQuery, cancellationToken);
        var memberCategory = member?.MembershipCategory;

        var balance = await _store.GetBalanceAsync(account.MemberId, cancellationToken);
        var allProducts = await _store.GetAllProductsAsync(cancellationToken);
        var vouchers = await _store.GetVouchersForAccountAsync(account.MemberId, cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var today = now.Date;

        var entitlements = new List<ProductEntitlementDto>();

        var allProductsById = allProducts.ToDictionary(p => p.ProductId, p => p);

        var activeProducts = allProducts
            .Where(p => p.IsActive)
            .Where(p => !p.ProductExpiresAtUtc.HasValue || p.ProductExpiresAtUtc.Value >= now)
            .Where(p => IsProductAllowedForMemberCategory(p, memberCategory))
            .Where(p =>
            {
                var cfg = _settings.EposBenefits.Products
                    .FirstOrDefault(c => Guid.Parse(c.ProductId) == p.ProductId);
                return cfg == null || cfg.IsActive;
            })
            .ToList();

        var redeemedByProduct = vouchers
            .Where(v =>
                v.RedeemedAtUtc.HasValue &&
                string.Equals(v.Status, "Redeemed", StringComparison.OrdinalIgnoreCase))
            .GroupBy(v => v.ProductIdGuid)
            .ToDictionary(g => g.Key, g => g.ToList());

        var productsByCategory = activeProducts
            .GroupBy(p => p.Category ?? string.Empty, StringComparer.OrdinalIgnoreCase);

        foreach (var categoryGroup in productsByCategory)
        {
            var productIdsInCategory = categoryGroup.Select(p => p.ProductId).ToHashSet();
            var vouchersForCategory = vouchers
                .Where(v => productIdsInCategory.Contains(v.ProductIdGuid))
                .ToList();

            var issuedVouchersForCategory = vouchersForCategory
                .Where(v =>
                    string.Equals(v.Status, "Issued", StringComparison.OrdinalIgnoreCase) &&
                    (!v.ExpiresAtUtc.HasValue || v.ExpiresAtUtc.Value >= now))
                .ToList();

            if (issuedVouchersForCategory.Any())
            {
                var chosenVoucher = issuedVouchersForCategory
                    .OrderBy(v => v.AllowanceCharge)
                    .ThenBy(v => v.ExpiresAtUtc ?? DateTimeOffset.MaxValue)
                    .ThenByDescending(v => v.RedemptionValue)
                    .First();

                if (!allProductsById.TryGetValue(chosenVoucher.ProductIdGuid, out var product))
                {
                    continue;
                }

                var allowanceCharge = chosenVoucher.AllowanceCharge;
                var hasFunding = allowanceCharge <= 0m || balance >= allowanceCharge;
                var hasQuantityLeft = HasQuantityLeftWithinWindow(product, redeemedByProduct, today);

                var isAvailable = hasFunding && hasQuantityLeft;

                entitlements.Add(new ProductEntitlementDto
                {
                    ProductId = product.ProductId,
                    VoucherId = chosenVoucher.VoucherId,
                    ProductCode = product.Code,
                    ProductName = product.DisplayName,
                    ProductImage = product.Image,
                    RedemptionValue = chosenVoucher.RedemptionValue,
                    AllowanceCharge = allowanceCharge,
                    IsAvailable = isAvailable
                });

                continue;
            }

            foreach (var product in categoryGroup)
            {
                var allowanceCharge = product.DefaultAllowanceCharge;
                var hasFunding = allowanceCharge <= 0m || balance >= allowanceCharge;
                var hasQuantityLeft = HasQuantityLeftWithinWindow(product, redeemedByProduct, today);

                var isAvailable = hasFunding && hasQuantityLeft;

                entitlements.Add(new ProductEntitlementDto
                {
                    ProductId = product.ProductId,
                    VoucherId = null,
                    ProductCode = product.Code,
                    ProductName = product.DisplayName,
                    ProductImage = product.Image,
                    RedemptionValue = product.DefaultRedemptionValue,
                    AllowanceCharge = allowanceCharge,
                    IsAvailable = isAvailable
                });
            }
        }

        return new AccountEntitlementsDto
        {
            MemberId = account.MemberId,
            DisplayName = account.DisplayName,
            CurrentBalance = balance,
            Products = entitlements
        };
    }

    private static bool HasQuantityLeftWithinWindow(
        EposProductEntity product,
        IDictionary<Guid, List<EposVoucherEntity>> redeemedByProduct,
        DateTime today)
    {
        var quantityAllowed = product.QuantityAllowed;
        var noRepeatDays = product.NoRepeatUseWithinDays;

        if (quantityAllowed <= 0 || noRepeatDays <= 0)
        {
            return true;
        }

        if (!redeemedByProduct.TryGetValue(product.ProductId, out var redeemedForProduct) || redeemedForProduct.Count == 0)
        {
            return true;
        }

        var windowStart = today.AddDays(1 - noRepeatDays);

        var redeemedInWindow = redeemedForProduct.Count(v =>
            v.RedeemedAtUtc!.Value.Date >= windowStart &&
            v.RedeemedAtUtc!.Value.Date <= today);

        return redeemedInWindow < quantityAllowed;
    }

    private static bool IsProductAllowedForMemberCategory(EposProductEntity product, string? memberCategory)
    {
        if (string.IsNullOrWhiteSpace(product.AllowedMembershipCategories))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(memberCategory))
        {
            return false;
        }

        var allowed = product.AllowedMembershipCategories
            .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(c => c.Trim());

        return allowed.Any(c =>
            string.Equals(c, memberCategory, StringComparison.OrdinalIgnoreCase));
    }
}
