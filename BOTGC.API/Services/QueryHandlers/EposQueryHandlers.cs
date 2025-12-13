using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.Encodings.Web;
using System.Threading;
using System.Threading.Tasks;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using QuestPDF;

namespace BOTGC.API.Services.QueryHandlers;


//#region Account Handlers

//public sealed class GetAccountSummaryQueryHandler
//    : QueryHandlerBase<GetAccountSummaryQuery, AccountSummaryDto?>
//{
//    private readonly IEposStore _store;

//    public GetAccountSummaryQueryHandler(IEposStore store)
//    {
//        _store = store ?? throw new ArgumentNullException(nameof(store));
//    }

//    public override async Task<AccountSummaryDto?> Handle(GetAccountSummaryQuery request, CancellationToken cancellationToken)
//    {
//        var account = await _store.GetAccountAsync(request.MemberId, cancellationToken);
//        if (account is null)
//        {
//            return null;
//        }

//        var balance = await _store.GetBalanceAsync(account.MemberId, cancellationToken);
//        var vouchers = await _store.GetVouchersForAccountAsync(account.MemberId, cancellationToken);
//        var redeemed = vouchers.Where(v =>
//            string.Equals(v.Status, "Redeemed", StringComparison.OrdinalIgnoreCase)
//            && v.RedeemedAtUtc.HasValue).ToList();

//        var totalRedemption = redeemed.Sum(v => v.RedemptionValue);
//        var bonusRedemption = redeemed.Where(v => v.IsBonus).Sum(v => v.RedemptionValue);
//        var lastRedemptionAt = redeemed.Count > 0 ? redeemed.Max(v => v.RedeemedAtUtc) : null;

//        return new AccountSummaryDto
//        {
//            MemberId = account.MemberId,
//            DisplayName = account.DisplayName,
//            CurrentBalance = balance,
//            TotalRedemptionValue = totalRedemption,
//            BonusRedemptionValue = bonusRedemption,
//            LastRedemptionAtUtc = lastRedemptionAt
//        };
//    }
//}

//public sealed class GetAccountBalanceQueryHandler
//    : QueryHandlerBase<GetAccountBalanceQuery, AccountBalanceDto?>
//{
//    private readonly IEposStore _store;

//    public GetAccountBalanceQueryHandler(IEposStore store)
//    {
//        _store = store ?? throw new ArgumentNullException(nameof(store));
//    }

//    public override async Task<AccountBalanceDto?> Handle(GetAccountBalanceQuery request, CancellationToken cancellationToken)
//    {
//        var account = await _store.GetAccountAsync(request.MemberId, cancellationToken);
//        if (account is null)
//        {
//            return null;
//        }

//        var balance = await _store.GetBalanceAsync(account.MemberId, cancellationToken);

//        return new AccountBalanceDto
//        {
//            MemberId = account.MemberId,
//            DisplayName = account.DisplayName,
//            CurrentBalance = balance
//        };
//    }
//}

//public sealed class GetAccountVouchersQueryHandler
//    : QueryHandlerBase<GetAccountVouchersQuery, List<AccountVoucherDto>>
//{
//    private readonly IEposStore _store;

//    public GetAccountVouchersQueryHandler(IEposStore store)
//    {
//        _store = store ?? throw new ArgumentNullException(nameof(store));
//    }

//    public override async Task<List<AccountVoucherDto>> Handle(GetAccountVouchersQuery request, CancellationToken cancellationToken)
//    {
//        var vouchers = await _store.GetVouchersForAccountAsync(request.MemberId, cancellationToken);

//        return vouchers
//            .OrderByDescending(v => v.IssuedAtUtc)
//            .Select(v => new AccountVoucherDto
//            {
//                VoucherId = v.VoucherId,
//                MemberId = v.MemberId,
//                VoucherCode = v.VoucherCode,
//                ProductId = v.ProductIdGuid,
//                ProductCode = v.ProductCode,
//                ProductName = v.ProductName,
//                RedemptionValue = v.RedemptionValue,
//                AllowanceCharge = v.AllowanceCharge,
//                IsBonus = v.IsBonus,
//                AwardReason = v.AwardReason,
//                Status = v.Status,
//                IssuedAtUtc = v.IssuedAtUtc,
//                RedeemedAtUtc = v.RedeemedAtUtc,
//                ExpiresAtUtc = v.ExpiresAtUtc,
//                InvoiceId = v.InvoiceId
//            })
//            .ToList();
//    }
//}

//#endregion

//    #region Product Handlers

//    public sealed class GetProductsQueryHandler
//        : QueryHandlerBase<GetProductsQuery, List<ProductDto>>
//    {
//        private readonly IEposStore _store;

//        public GetProductsQueryHandler(IEposStore store)
//        {
//            _store = store ?? throw new ArgumentNullException(nameof(store));
//        }

//        public override async Task<List<ProductDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
//        {
//            var products = await _store.GetAllProductsAsync(cancellationToken);

//            return products
//                .OrderBy(p => p.Category)
//                .ThenBy(p => p.DisplayName)
//                .Select(p => new ProductDto
//                {
//                    ProductId = p.ProductId,
//                    Code = p.Code,
//                    DisplayName = p.DisplayName,
//                    Description = p.Description,
//                    Category = p.Category,
//                    DefaultRedemptionValue = p.DefaultRedemptionValue,
//                    DefaultAllowanceCharge = p.DefaultAllowanceCharge,
//                    IsActive = p.IsActive
//                })
//                .ToList();
//        }
//    }

//    #endregion

//    #region Voucher Handlers


//    public sealed class RedeemVoucherCommandHandler
//     : QueryHandlerBase<RedeemVoucherCommand, VoucherRedemptionResultDto?>
//    {
//        private readonly IEposStore _store;
//        private readonly IBenefitsQrTokenService _benefitsQrTokenService;
//        private readonly ILogger<RedeemVoucherCommandHandler> _logger;

//        public RedeemVoucherCommandHandler(
//            IEposStore store,
//            IBenefitsQrTokenService benefitsQrTokenService,
//            ILogger<RedeemVoucherCommandHandler> logger)
//        {
//            _store = store ?? throw new ArgumentNullException(nameof(store));
//            _benefitsQrTokenService = benefitsQrTokenService ?? throw new ArgumentNullException(nameof(benefitsQrTokenService));
//            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
//        }

//        public override async Task<VoucherRedemptionResultDto?> Handle(
//            RedeemVoucherCommand request,
//            CancellationToken cancellationToken)
//        {
//            // 1. Decrypt QR token
//            var tokenPayload = _benefitsQrTokenService.TryDecrypt(request.Code);
//            if (tokenPayload is null)
//            {
//                _logger.LogWarning("RedeemVoucher: invalid QR token.");
//                return null;
//            }

//            _logger.LogInformation(
//                "RedeemVoucher: token for MemberId {MemberId}, ProductId {ProductId}, IssuedAt {IssuedAt}.",
//                tokenPayload.MemberId,
//                tokenPayload.ProductId,
//                tokenPayload.IssuedAtUtc);

//            // 2. Get account
//            var account = await _store.GetAccountAsync(tokenPayload.MemberId, cancellationToken);
//            if (account is null)
//            {
//                _logger.LogWarning(
//                    "RedeemVoucher: no account found for MemberId {MemberId}.",
//                    tokenPayload.MemberId);
//                return null;
//            }

//            // 3. Try to find existing voucher (idempotent key)
//            var existingVouchers = await _store.GetVouchersForAccountAsync(tokenPayload.MemberId, cancellationToken);

//            var voucher = existingVouchers.FirstOrDefault(v =>
//                v.ProductIdGuid == tokenPayload.ProductId &&
//                v.IssuedAtUtc == tokenPayload.IssuedAtUtc);

//            // 4. Create voucher if it doesn't exist yet
//            if (voucher is null)
//            {
//                var product = await _store.GetProductAsync(tokenPayload.ProductId, cancellationToken);
//                if (product is null || !product.IsActive)
//                {
//                    _logger.LogWarning(
//                        "RedeemVoucher: product {ProductId} not found or inactive.",
//                        tokenPayload.ProductId);
//                    return null;
//                }

//                voucher = await _store.CreateVoucherAsync(
//                    tokenPayload.MemberId,
//                    product,
//                    isBonus: false,                 // always charge the account
//                    awardReason: "QR Redemption",
//                    voucherCode: Guid.NewGuid().ToString("N"),
//                    issuedAtUtc: tokenPayload.IssuedAtUtc,
//                    expiresAtUtc: null,
//                    ct: cancellationToken);

//                _logger.LogInformation(
//                    "RedeemVoucher: created voucher {VoucherId} for MemberId {MemberId}. " +
//                    "RedemptionValue={RedemptionValue}, AllowanceCharge={AllowanceCharge}",
//                    voucher.VoucherId,
//                    tokenPayload.MemberId,
//                    voucher.RedemptionValue,
//                    voucher.AllowanceCharge);
//            }

//            // 5. Validate state / expiry
//            if (!string.Equals(voucher.Status, "Issued", StringComparison.OrdinalIgnoreCase))
//            {
//                _logger.LogWarning(
//                    "RedeemVoucher: voucher {VoucherId} not in Issued state (Status={Status}).",
//                    voucher.VoucherId,
//                    voucher.Status);
//                return null;
//            }

//            if (voucher.ExpiresAtUtc.HasValue && voucher.ExpiresAtUtc.Value < DateTimeOffset.UtcNow)
//            {
//                _logger.LogWarning(
//                    "RedeemVoucher: voucher {VoucherId} expired at {ExpiresAtUtc}.",
//                    voucher.VoucherId,
//                    voucher.ExpiresAtUtc);
//                return null;
//            }

//            // 6. Redeem + debit
//            var beforeBalance = await _store.GetBalanceAsync(account.MemberId, cancellationToken);
//            var redeemedAt = DateTimeOffset.UtcNow;

//            var updatedVoucher = await _store.RedeemVoucherAsync(
//                voucher,
//                account,
//                redeemedAt,
//                cancellationToken);

//            if (updatedVoucher is null)
//            {
//                _logger.LogWarning(
//                    "RedeemVoucher: voucher {VoucherId} could not be redeemed.",
//                    voucher.VoucherId);
//                return null;
//            }

//            var afterBalance = await _store.GetBalanceAsync(account.MemberId, cancellationToken);
//            if (afterBalance < 0m)
//            {
//                afterBalance = 0m;
//            }

//            _logger.LogInformation(
//                "RedeemVoucher: voucher {VoucherId} redeemed. Balance {Before} -> {After}.",
//                updatedVoucher.VoucherId,
//                beforeBalance,
//                afterBalance);

//            // 7. Build result – no bonus flags, just the money
//            return new VoucherRedemptionResultDto
//            {
//                VoucherId = updatedVoucher.VoucherId,
//                AccountId = account.MemberId,
//                ProductId = updatedVoucher.ProductIdGuid,
//                ProductCode = updatedVoucher.ProductCode,
//                ProductName = updatedVoucher.ProductName,
//                RedemptionValue = updatedVoucher.RedemptionValue,
//                AllowanceCharge = updatedVoucher.AllowanceCharge,
//                RedeemedAt = redeemedAt.UtcDateTime,
//                Status = updatedVoucher.Status
//            };
//        }
//    }

//    public sealed class AwardBonusVouchersCommandHandler
//        : QueryHandlerBase<AwardBonusVouchersCommand, AwardBonusVouchersResultDto>
//    {
//        private readonly IEposStore _store;

//        public AwardBonusVouchersCommandHandler(IEposStore store)
//        {
//            _store = store ?? throw new ArgumentNullException(nameof(store));
//        }

//        public override async Task<AwardBonusVouchersResultDto> Handle(AwardBonusVouchersCommand request, CancellationToken cancellationToken)
//        {
//            var result = new AwardBonusVouchersResultDto();

//            if (request.Request.Items is null || request.Request.Items.Count == 0)
//            {
//                return result;
//            }

//            foreach (var item in request.Request.Items)
//            {
//                var product = await _store.GetProductAsync(item.ProductId, cancellationToken);
//                if (product is null || !product.IsActive)
//                {
//                    result.Items.Add(new AwardBonusVouchersResultItemDto
//                    {
//                        MemberId = item.MemberId,
//                        ProductId = item.ProductId,
//                        QuantityRequested = item.Quantity,
//                        QuantityCreated = 0
//                    });

//                    continue;
//                }

//                var created = 0;
//                for (var i = 0; i < item.Quantity; i++)
//                {
//                    var code = Guid.NewGuid().ToString("N");
//                    var issuedAt = DateTimeOffset.UtcNow;

//                    await _store.CreateVoucherAsync(
//                        item.MemberId,
//                        product,
//                        isBonus: true,
//                        awardReason: item.AwardReason,
//                        voucherCode: code,
//                        issuedAtUtc: issuedAt,
//                        expiresAtUtc: item.ExpiresAtUtc,
//                        ct: cancellationToken);

//                    created++;
//                }

//                result.Items.Add(new AwardBonusVouchersResultItemDto
//                {
//                    MemberId = item.MemberId,
//                    ProductId = item.ProductId,
//                    QuantityRequested = item.Quantity,
//                    QuantityCreated = created
//                });

//                result.TotalRequested += item.Quantity;
//                result.TotalCreated += created;
//            }

//            return result;
//        }
//    }

//    #endregion

//    #region Pro Shop Invoice Handlers

//    public sealed class GetProShopInvoiceSummaryQueryHandler
//        : QueryHandlerBase<GetProShopInvoiceSummaryQuery, ProShopInvoiceSummaryDto>
//    {
//        private readonly IEposStore _store;

//        public GetProShopInvoiceSummaryQueryHandler(IEposStore store)
//        {
//            _store = store ?? throw new ArgumentNullException(nameof(store));
//        }

//        public override async Task<ProShopInvoiceSummaryDto> Handle(GetProShopInvoiceSummaryQuery request, CancellationToken cancellationToken)
//        {
//            var vouchers = await _store.GetRedeemedUninvoicedVouchersAsync(
//                request.FromUtc,
//                request.ToUtc,
//                cancellationToken);

//            return new ProShopInvoiceSummaryDto
//            {
//                FromUtc = request.FromUtc,
//                ToUtc = request.ToUtc,
//                VoucherCount = vouchers.Count,
//                TotalRedemptionValue = vouchers.Sum(v => v.RedemptionValue)
//            };
//        }
//    }

//    public sealed class GetProShopInvoiceDetailQueryHandler
//        : QueryHandlerBase<GetProShopInvoiceDetailQuery, ProShopInvoiceDetailDto?>
//    {
//        private readonly IEposStore _store;

//        public GetProShopInvoiceDetailQueryHandler(IEposStore store)
//        {
//            _store = store ?? throw new ArgumentNullException(nameof(store));
//        }

//        public override async Task<ProShopInvoiceDetailDto?> Handle(GetProShopInvoiceDetailQuery request, CancellationToken cancellationToken)
//        {
//            var invoice = await _store.GetInvoiceAsync(request.InvoiceId, cancellationToken);
//            if (invoice is null)
//            {
//                return null;
//            }

//            return new ProShopInvoiceDetailDto
//            {
//                InvoiceId = invoice.InvoiceId,
//                CreatedAtUtc = invoice.CreatedAtUtc,
//                FromUtc = invoice.FromUtc,
//                ToUtc = invoice.ToUtc,
//                Description = invoice.Description,
//                TotalAmount = invoice.TotalAmount
//            };
//        }
//    }

//    public sealed class GetProShopInvoiceLinesQueryHandler
//        : QueryHandlerBase<GetProShopInvoiceLinesQuery, List<ProShopInvoiceLineDto>>
//    {
//        private readonly IEposStore _store;

//        public GetProShopInvoiceLinesQueryHandler(IEposStore store)
//        {
//            _store = store ?? throw new ArgumentNullException(nameof(store));
//        }

//        public override async Task<List<ProShopInvoiceLineDto>> Handle(GetProShopInvoiceLinesQuery request, CancellationToken cancellationToken)
//        {
//            var lines = await _store.GetInvoiceLinesAsync(request.InvoiceId, cancellationToken);

//            return lines
//                .OrderBy(l => l.ProductName)
//                .Select(l => new ProShopInvoiceLineDto
//                {
//                    InvoiceId = l.InvoiceId,
//                    ProductId = l.ProductIdGuid,
//                    ProductCode = l.ProductCode,
//                    ProductName = l.ProductName,
//                    Quantity = l.Quantity,
//                    RedemptionValuePerUnit = l.RedemptionValuePerUnit,
//                    TotalRedemptionValue = l.TotalRedemptionValue
//                })
//                .ToList();
//        }
//    }

//    #endregion

//    #region Subscription Credit Handler



//    #endregion





//public sealed class GenerateVoucherCommandHandler
//    : QueryHandlerBase<GenerateVoucherCommand, AccountVoucherDto?>
//{
//    private readonly IEposStore _store;

//    public GenerateVoucherCommandHandler(IEposStore store)
//    {
//        _store = store ?? throw new ArgumentNullException(nameof(store));
//    }

//    public override async Task<AccountVoucherDto?> Handle(GenerateVoucherCommand request, CancellationToken cancellationToken)
//    {
//        var account = await _store.GetAccountAsync(request.MemberId, cancellationToken);
//        if (account is null)
//        {
//            return null;
//        }

//        var product = await _store.GetProductAsync(request.ProductId, cancellationToken);
//        if (product is null || !product.IsActive)
//        {
//            return null;
//        }

//        if (!request.IsBonus && product.DefaultAllowanceCharge > 0)
//        {
//            var balance = await _store.GetBalanceAsync(account.MemberId, cancellationToken);
//            if (balance < product.DefaultAllowanceCharge)
//            {
//                return null;
//            }
//        }

//        var code = Guid.NewGuid().ToString("N");
//        var issuedAt = DateTimeOffset.UtcNow;

//        var voucher = await _store.CreateVoucherAsync(
//            account.MemberId,
//            product,
//            request.IsBonus,
//            request.AwardReason,
//            code,
//            issuedAt,
//            request.ExpiresAtUtc,
//            cancellationToken);

//        return new AccountVoucherDto
//        {
//            VoucherId = voucher.VoucherId,
//            MemberId = voucher.MemberId,
//            VoucherCode = voucher.VoucherCode,
//            ProductId = voucher.ProductIdGuid,
//            ProductCode = voucher.ProductCode,
//            ProductName = voucher.ProductName,
//            RedemptionValue = voucher.RedemptionValue,
//            AllowanceCharge = voucher.AllowanceCharge,
//            IsBonus = voucher.IsBonus,
//            AwardReason = voucher.AwardReason,
//            Status = voucher.Status,
//            IssuedAtUtc = voucher.IssuedAtUtc,
//            RedeemedAtUtc = voucher.RedeemedAtUtc,
//            ExpiresAtUtc = voucher.ExpiresAtUtc,
//            InvoiceId = voucher.InvoiceId
//        };
//    }
//}