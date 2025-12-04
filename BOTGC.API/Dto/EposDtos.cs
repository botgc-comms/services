using System;
using System.Collections.Generic;

namespace BOTGC.API.Dto
{
    public sealed class AccountSummaryDto
    {
        public int MemberId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int SchemeYear { get; set; }

        public decimal OpeningBalance { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal BenefitCap { get; set; }
        public decimal AllowanceUsed { get; set; }

        public decimal TotalRedemptionValue { get; set; }
        public decimal BonusRedemptionValue { get; set; }

        public DateTimeOffset? LastRedemptionAtUtc { get; set; }
    }

    public sealed class AccountBalanceDto
    {
        public int MemberId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public int SchemeYear { get; set; }

        public decimal OpeningBalance { get; set; }
        public decimal CurrentBalance { get; set; }

        public decimal BenefitCap { get; set; }
        public decimal AllowanceUsed { get; set; }
        public decimal RemainingAllowance { get; set; }
    }

    public sealed class ProductEntitlementDto
    {
        public Guid ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;

        public decimal RedemptionValue { get; set; }
        public decimal AllowanceCharge { get; set; }
    }

    public sealed class AccountEntitlementsDto
    {
        public int MemberId { get; set; }
        public string DisplayName { get; set; } = string.Empty;

        public decimal CurrentBalance { get; set; }

        public List<ProductEntitlementDto> Products { get; set; } = new();
    }

    public sealed class AccountVoucherDto
    {
        public Guid VoucherId { get; set; }
        public int MemberId { get; set; }

        public string VoucherCode { get; set; } = string.Empty;

        public Guid ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;

        public decimal RedemptionValue { get; set; }
        public decimal AllowanceCharge { get; set; }

        public bool IsBonus { get; set; }
        public string? AwardReason { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTimeOffset IssuedAtUtc { get; set; }
        public DateTimeOffset? RedeemedAtUtc { get; set; }
        public DateTimeOffset? ExpiresAtUtc { get; set; }

        public string? InvoiceId { get; set; }
    }

    public sealed class GenerateVoucherRequestDto
    {
        public int MemberId { get; set; }
        public Guid ProductId { get; set; }
        public bool IsBonus { get; set; }
        public string? AwardReason { get; set; }
        public DateTimeOffset? ExpiresAtUtc { get; set; }
    }

    public sealed class AwardVoucherRequestItemDto
    {
        public int MemberId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public string? AwardReason { get; set; }
        public DateTimeOffset? ExpiresAtUtc { get; set; }
    }

    public sealed class AwardBonusVouchersRequestDto
    {
        public List<AwardVoucherRequestItemDto> Items { get; set; } = new();
    }

    public sealed class AwardBonusVouchersResultItemDto
    {
        public int MemberId { get; set; }
        public Guid ProductId { get; set; }
        public int QuantityRequested { get; set; }
        public int QuantityCreated { get; set; }
    }

    public sealed class AwardBonusVouchersResultDto
    {
        public int TotalRequested { get; set; }
        public int TotalCreated { get; set; }
        public List<AwardBonusVouchersResultItemDto> Items { get; set; } = new();
    }

    public sealed class VoucherRedemptionResultDto
    {
        public Guid VoucherId { get; set; }
        public int AccountId { get; set; }

        public Guid ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;

        public decimal RedemptionValue { get; set; }
        public decimal AllowanceCharge { get; set; }

        public DateTime RedeemedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }


    public sealed class ProductDto
    {
        public Guid ProductId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Category { get; set; } = string.Empty;

        public decimal DefaultRedemptionValue { get; set; }
        public decimal DefaultAllowanceCharge { get; set; }

        public bool IsActive { get; set; }
        public DateTimeOffset? ProductExpiresAtUtc { get; set; }
    }

    public sealed class ProShopInvoiceSummaryDto
    {
        public DateTimeOffset? FromUtc { get; set; }
        public DateTimeOffset? ToUtc { get; set; }

        public int VoucherCount { get; set; }
        public decimal TotalRedemptionValue { get; set; }
    }

    public sealed class ProShopInvoiceDetailDto
    {
        public Guid InvoiceId { get; set; }
        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset? FromUtc { get; set; }
        public DateTimeOffset? ToUtc { get; set; }
        public string? Description { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public sealed class ProShopInvoiceLineDto
    {
        public Guid InvoiceId { get; set; }
        public Guid ProductId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;

        public int Quantity { get; set; }
        public decimal RedemptionValuePerUnit { get; set; }
        public decimal TotalRedemptionValue { get; set; }
    }

    public sealed class BenefitsQrPayloadDto
    {
        public int MemberId { get; set; }
        public Guid ProductId { get; set; }
        public DateTimeOffset IssuedAtUtc { get; set; }
        public string? CallbackUrl { get; set; }
        public Guid? VoucherId { get; set; }
    }
}
