using Azure;
using Azure.Data.Tables;

namespace BOTGC.API.Models;

public sealed class EposAccountEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "Account";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public int MemberId { get; set; }
    public string DisplayName { get; set; } = string.Empty;

    public static string RowKeyFor(int memberId) => memberId.ToString();
}

public sealed class EposAccountTransactionEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty; // MemberId as string
    public string RowKey { get; set; } = string.Empty;       // Ticks as string
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public int MemberId { get; set; }

    /// <summary>Positive for credit, negative for debit.</summary>
    public double Amount { get; set; }

    public string Type { get; set; } = string.Empty; // "Credit" / "Debit"
    public string Reason { get; set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; set; }

    public string? VoucherId { get; set; }
    public string? ProductId { get; set; }

    public Guid? VoucherIdGuid => string.IsNullOrWhiteSpace(VoucherId) ? null : Guid.Parse(VoucherId!);
    public Guid? ProductIdGuid => string.IsNullOrWhiteSpace(ProductId) ? null : Guid.Parse(ProductId!);

    public static string PartitionForMember(int memberId) => memberId.ToString();
    public static string RowKeyFor(DateTimeOffset atUtc) => atUtc.UtcDateTime.Ticks.ToString("D19");
}

public sealed class EposProductEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "Product";
    public string RowKey { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string Code { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Category { get; set; } = string.Empty;

    public decimal DefaultRedemptionValue { get; set; }
    public decimal DefaultAllowanceCharge { get; set; }

    public bool IsActive { get; set; } = true;

    public string? AllowedMembershipCategories { get; set; }

    public DateTimeOffset? ProductExpiresAtUtc { get; set; }

    public Guid ProductId => Guid.Parse(RowKey);
    public static string RowKeyFor(Guid productId) => productId.ToString("N");
    
}

public sealed class EposVoucherEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "Voucher";
    public string RowKey { get; set; } = string.Empty; // VoucherId as N
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string VoucherCode { get; set; } = string.Empty;

    public string AccountId { get; set; } = string.Empty; // MemberId as string
    public string ProductId { get; set; } = string.Empty; // ProductId as N
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    public decimal RedemptionValue { get; set; }
    public decimal AllowanceCharge { get; set; }

    public bool IsBonus { get; set; }
    public string? AwardReason { get; set; }

    public string Status { get; set; } = "Issued"; // "Issued" / "Redeemed" / "Cancelled"

    public DateTimeOffset IssuedAtUtc { get; set; }
    public DateTimeOffset? RedeemedAtUtc { get; set; }
    public string? RedeemedByUserId { get; set; }
    public string? RedeemedSource { get; set; }

    public string? InvoiceId { get; set; }
    public DateTimeOffset? ExpiresAtUtc { get; set; }

    public Guid VoucherId => Guid.Parse(RowKey);

    public int MemberId => int.Parse(AccountId);
    public Guid ProductIdGuid => Guid.Parse(ProductId);

    public static string RowKeyFor(Guid voucherId) => voucherId.ToString("N");
}

public sealed class EposVoucherCodeIndexEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "VoucherCode";
    public string RowKey { get; set; } = string.Empty; // VoucherCode
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string AccountId { get; set; } = string.Empty; // MemberId as string
    public string VoucherId { get; set; } = string.Empty; // VoucherId as N

    public int MemberId => int.Parse(AccountId);
    public Guid VoucherIdGuid => Guid.Parse(VoucherId);

    public static string RowKeyFor(string code) => code;
}

public sealed class EposProShopInvoiceEntity : ITableEntity
{
    public string PartitionKey { get; set; } = "Invoice";
    public string RowKey { get; set; } = string.Empty; // InvoiceId as N
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? FromUtc { get; set; }
    public DateTimeOffset? ToUtc { get; set; }
    public string? Description { get; set; }
    public decimal TotalAmount { get; set; }

    public Guid InvoiceId => Guid.Parse(RowKey);
    public static string RowKeyFor(Guid invoiceId) => invoiceId.ToString("N");
}

public sealed class EposProShopInvoiceLineEntity : ITableEntity
{
    public string PartitionKey { get; set; } = string.Empty; // InvoiceId as N
    public string RowKey { get; set; } = string.Empty;       // ProductId as N
    public DateTimeOffset? Timestamp { get; set; }
    public ETag ETag { get; set; }

    public string ProductId { get; set; } = string.Empty; // ProductId as N
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;

    public int Quantity { get; set; }
    public decimal RedemptionValuePerUnit { get; set; }
    public decimal TotalRedemptionValue { get; set; }

    public Guid InvoiceId => Guid.Parse(PartitionKey);
    public Guid ProductIdGuid => Guid.Parse(ProductId);

    public static string PartitionForInvoice(Guid invoiceId) => invoiceId.ToString("N");
    public static string RowKeyForProduct(Guid productId) => productId.ToString("N");
}
