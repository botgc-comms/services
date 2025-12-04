using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries;

// Accounts

public sealed record EposQueries(int MemberId)
    : QueryBase<AccountSummaryDto?>;

public sealed record GetAccountSummaryQuery(int MemberId)
    : QueryBase<AccountSummaryDto?>;    

public sealed record GetAccountBalanceQuery(int MemberId)
    : QueryBase<AccountBalanceDto?>;

public sealed record GetAccountEntitlementsQuery(int MemberId)
    : QueryBase<AccountEntitlementsDto?>;

public sealed record GetAccountVouchersQuery(int MemberId)
    : QueryBase<List<AccountVoucherDto>>;

// Products

public sealed record GetProductsQuery()
    : QueryBase<List<ProductDto>>;

// Vouchers

public sealed record GenerateVoucherCommand(
    int MemberId,
    Guid ProductId,
    bool IsBonus,
    string? AwardReason,
    DateTimeOffset? ExpiresAtUtc
) : QueryBase<AccountVoucherDto?>;

public sealed record RedeemVoucherCommand(
    string Code
) : QueryBase<VoucherRedemptionResultDto?>;

public sealed record AwardBonusVouchersCommand(
    AwardBonusVouchersRequestDto Request
) : QueryBase<AwardBonusVouchersResultDto>;

// Pro shop invoices

public sealed record GetProShopInvoiceSummaryQuery(
    DateTimeOffset? FromUtc,
    DateTimeOffset? ToUtc
) : QueryBase<ProShopInvoiceSummaryDto>;

public sealed record GetProShopInvoiceDetailQuery(
    Guid InvoiceId
) : QueryBase<ProShopInvoiceDetailDto?>;

public sealed record GetProShopInvoiceLinesQuery(
    Guid InvoiceId
) : QueryBase<List<ProShopInvoiceLineDto>>;

// Subscription credit – plain IRequest<bool> (not a QueryBase)

public sealed record UpdateBenefitsAccountCommand(
    int MemberId,
    int? SchemeYear,
    decimal Amount
) : QueryBase<bool>;

