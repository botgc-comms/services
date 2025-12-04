using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;

namespace BOTGC.API.Services;

public sealed class EposStore : IEposStore
{
    private readonly ITableStore<EposAccountEntity> _accounts;
    private readonly ITableStore<EposAccountTransactionEntity> _transactions;
    private readonly ITableStore<EposProductEntity> _products;
    private readonly ITableStore<EposVoucherEntity> _vouchers;
    private readonly ITableStore<EposVoucherCodeIndexEntity> _voucherCodes;
    private readonly ITableStore<EposProShopInvoiceEntity> _invoices;
    private readonly ITableStore<EposProShopInvoiceLineEntity> _invoiceLines;

    public EposStore(
        ITableStore<EposAccountEntity> accounts,
        ITableStore<EposAccountTransactionEntity> transactions,
        ITableStore<EposProductEntity> products,
        ITableStore<EposVoucherEntity> vouchers,
        ITableStore<EposVoucherCodeIndexEntity> voucherCodes,
        ITableStore<EposProShopInvoiceEntity> invoices,
        ITableStore<EposProShopInvoiceLineEntity> invoiceLines)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _transactions = transactions ?? throw new ArgumentNullException(nameof(transactions));
        _products = products ?? throw new ArgumentNullException(nameof(products));
        _vouchers = vouchers ?? throw new ArgumentNullException(nameof(vouchers));
        _voucherCodes = voucherCodes ?? throw new ArgumentNullException(nameof(voucherCodes));
        _invoices = invoices ?? throw new ArgumentNullException(nameof(invoices));
        _invoiceLines = invoiceLines ?? throw new ArgumentNullException(nameof(invoiceLines));
    }

    private static string AccountRowKey(int memberId) => memberId.ToString();
    private static string TransactionPartition(int memberId) => memberId.ToString();
    private static string TransactionRowKey(DateTimeOffset atUtc) => atUtc.UtcDateTime.Ticks.ToString("D19");
    private static string ProductRowKey(Guid productId) => productId.ToString("N");
    private static string VoucherRowKey(Guid voucherId) => voucherId.ToString("N");
    private static string VoucherCodeRowKey(string code) => code;
    private static string InvoiceRowKey(Guid invoiceId) => invoiceId.ToString("N");
    private static string InvoiceLinePartition(Guid invoiceId) => invoiceId.ToString("N");
    private static string InvoiceLineRowKey(Guid productId) => productId.ToString("N");

    #region Accounts / Ledger

    public Task<EposAccountEntity?> GetAccountAsync(int memberId, CancellationToken ct)
    {
        return _accounts.GetAsync("Account", AccountRowKey(memberId), ct);
    }

    public async Task<EposAccountEntity> CreateOrGetAccountAsync(
        int memberId,
        string displayName,
        CancellationToken ct)
    {
        var existing = await GetAccountAsync(memberId, ct);
        if (existing is not null)
        {
            return existing;
        }

        var account = new EposAccountEntity
        {
            PartitionKey = "Account",
            RowKey = AccountRowKey(memberId),
            MemberId = memberId,
            DisplayName = displayName ?? string.Empty
        };

        await _accounts.UpsertAsync(account, ct);
        return account;
    }

    public Task UpsertAccountAsync(EposAccountEntity account, CancellationToken ct)
    {
        return _accounts.UpsertAsync(account, ct);
    }

    public async Task<EposAccountTransactionEntity> CreditAccountAsync(
        EposAccountEntity account,
        decimal amount,
        string reason,
        CancellationToken ct)
    {
        if (amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Credit amount must be positive.");
        }

        var now = DateTimeOffset.UtcNow;

        var txn = new EposAccountTransactionEntity
        {
            PartitionKey = TransactionPartition(account.MemberId),
            RowKey = TransactionRowKey(now),
            MemberId = account.MemberId,
            Amount = (double)amount,
            Type = "Credit",
            Reason = reason ?? string.Empty,
            CreatedAtUtc = now,
            VoucherId = null,
            ProductId = null
        };

        await _transactions.UpsertAsync(txn, ct);
        return txn;
    }

    public async Task<EposAccountTransactionEntity> DebitAccountAsync(
        EposAccountEntity account,
        decimal amount,
        string reason,
        Guid? voucherId,
        Guid? productId,
        CancellationToken ct)
    {
        if (amount <= 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Debit amount must be positive.");
        }

        var now = DateTimeOffset.UtcNow;

        var txn = new EposAccountTransactionEntity
        {
            PartitionKey = TransactionPartition(account.MemberId),
            RowKey = TransactionRowKey(now),
            MemberId = account.MemberId,
            Amount = (double)-amount,
            Type = "Debit",
            Reason = reason ?? string.Empty,
            CreatedAtUtc = now,
            VoucherId = voucherId?.ToString("N"),
            ProductId = productId?.ToString("N")
        };

        await _transactions.UpsertAsync(txn, ct);
        return txn;
    }

    public async Task<decimal> GetBalanceAsync(int memberId, CancellationToken ct)
    {
        var txns = await GetAccountTransactionsAsync(memberId, null, null, ct);
        return (decimal)txns.Sum(t => t.Amount);
    }

    public async Task<IReadOnlyList<EposAccountTransactionEntity>> GetAccountTransactionsAsync(
        int memberId,
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken ct)
    {
        var pk = TransactionPartition(memberId);

        var list = new List<EposAccountTransactionEntity>();
        await foreach (var t in _transactions.QueryByPartitionAsync(pk, null, ct))
        {
            list.Add(t);
        }

        if (fromUtc.HasValue)
        {
            list = list.Where(t => t.CreatedAtUtc >= fromUtc.Value).ToList();
        }

        if (toUtc.HasValue)
        {
            list = list.Where(t => t.CreatedAtUtc <= toUtc.Value).ToList();
        }

        return list;
    }

    #endregion

    #region Products

    public Task<EposProductEntity?> GetProductAsync(Guid productId, CancellationToken ct)
    {
        return _products.GetAsync("Product", ProductRowKey(productId), ct);
    }

    public async Task<List<EposProductEntity>> GetAllProductsAsync(CancellationToken ct)
    {
        var list = new List<EposProductEntity>();
        await foreach (var p in _products.QueryByPartitionAsync("Product", null, ct))
        {
            list.Add(p);
        }
        return list;
    }

    public async Task<EposProductEntity> UpsertProductAsync(EposProductEntity product, CancellationToken ct)
    {
        await _products.UpsertAsync(product, ct);
        return product;
    }

    #endregion

    #region Vouchers

    public async Task<List<EposVoucherEntity>> GetVouchersForAccountAsync(int memberId, CancellationToken ct)
    {
        var list = new List<EposVoucherEntity>();
        var filter = $"AccountId eq '{memberId}'";
        await foreach (var v in _vouchers.QueryByPartitionAsync("Voucher", filter, ct))
        {
            list.Add(v);
        }
        return list;
    }

    public Task<EposVoucherEntity?> GetVoucherAsync(Guid voucherId, CancellationToken ct)
    {
        return _vouchers.GetAsync("Voucher", VoucherRowKey(voucherId), ct);
    }

    public async Task<(EposVoucherEntity? Voucher, EposAccountEntity? Account)> GetVoucherByCodeAsync(string code, CancellationToken ct)
    {
        var index = await _voucherCodes.GetAsync("VoucherCode", VoucherCodeRowKey(code), ct);
        if (index is null)
        {
            return (null, null);
        }

        if (!int.TryParse(index.AccountId, out var memberId))
        {
            return (null, null);
        }

        var voucherGuid = index.VoucherIdGuid;
        var voucher = await GetVoucherAsync(voucherGuid, ct);
        if (voucher is null)
        {
            return (null, null);
        }

        var account = await GetAccountAsync(memberId, ct);
        return (voucher, account);
    }

    public async Task<EposVoucherEntity> CreateVoucherAsync(
        int memberId,
        EposProductEntity product,
        bool isBonus,
        string? awardReason,
        string voucherCode,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset? expiresAtUtc,
        CancellationToken ct)
    {
        var voucherId = Guid.NewGuid();

        var voucher = new EposVoucherEntity
        {
            PartitionKey = "Voucher",
            RowKey = VoucherRowKey(voucherId),
            VoucherCode = voucherCode,
            AccountId = memberId.ToString(),
            ProductId = product.ProductId.ToString("N"),
            ProductCode = product.Code ?? string.Empty,
            ProductName = product.DisplayName ?? string.Empty,
            RedemptionValue = product.DefaultRedemptionValue,
            AllowanceCharge = isBonus ? 0m : product.DefaultAllowanceCharge,
            IsBonus = isBonus,
            AwardReason = awardReason,
            Status = "Issued",
            IssuedAtUtc = issuedAtUtc,
            RedeemedAtUtc = null,
            RedeemedByUserId = null,
            RedeemedSource = null,
            InvoiceId = null,
            ExpiresAtUtc = expiresAtUtc
        };

        var index = new EposVoucherCodeIndexEntity
        {
            PartitionKey = "VoucherCode",
            RowKey = VoucherCodeRowKey(voucherCode),
            AccountId = memberId.ToString(),
            VoucherId = voucherId.ToString("N")
        };

        await _vouchers.UpsertAsync(voucher, ct);
        await _voucherCodes.UpsertAsync(index, ct);

        return voucher;
    }

    public async Task<EposVoucherEntity?> RedeemVoucherAsync(
        EposVoucherEntity voucher,
        EposAccountEntity account,
        DateTimeOffset redeemedAtUtc,
        CancellationToken ct)
    {
        if (!string.Equals(voucher.Status, "Issued", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        voucher.Status = "Redeemed";
        voucher.RedeemedAtUtc = redeemedAtUtc;

        if (!voucher.IsBonus && voucher.AllowanceCharge > 0m)
        {
            await DebitAccountAsync(
                account,
                voucher.AllowanceCharge,
                $"Voucher redemption: {voucher.ProductCode} {voucher.ProductName}",
                voucher.VoucherId,
                voucher.ProductIdGuid,
                ct);
        }

        await _vouchers.UpsertAsync(voucher, ct);
        await _accounts.UpsertAsync(account, ct);

        return voucher;
    }

    public async Task<List<EposVoucherEntity>> GetRedeemedUninvoicedVouchersAsync(
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken ct)
    {
        var list = new List<EposVoucherEntity>();
        await foreach (var v in _vouchers.QueryByPartitionAsync("Voucher", null, ct))
        {
            list.Add(v);
        }

        var query = list.Where(v =>
            string.Equals(v.Status, "Redeemed", StringComparison.OrdinalIgnoreCase) &&
            v.RedeemedAtUtc.HasValue &&
            string.IsNullOrWhiteSpace(v.InvoiceId));

        if (fromUtc.HasValue)
        {
            query = query.Where(v => v.RedeemedAtUtc!.Value >= fromUtc.Value);
        }

        if (toUtc.HasValue)
        {
            query = query.Where(v => v.RedeemedAtUtc!.Value <= toUtc.Value);
        }

        return query.ToList();
    }

    #endregion

    #region Invoices

    public async Task<(EposProShopInvoiceEntity Invoice, List<EposProShopInvoiceLineEntity> Lines)> CreateInvoiceFromVouchersAsync(
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        string? description,
        IReadOnlyList<EposVoucherEntity> vouchers,
        CancellationToken ct)
    {
        var invoiceId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        var groups = vouchers
            .GroupBy(v => v.ProductIdGuid)
            .Select(g =>
            {
                var first = g.First();
                var qty = g.Count();
                var perUnit = first.RedemptionValue;
                return new
                {
                    ProductId = g.Key,
                    first.ProductCode,
                    first.ProductName,
                    Quantity = qty,
                    RedemptionValuePerUnit = perUnit,
                    Total = perUnit * qty
                };
            })
            .ToList();

        var totalAmount = groups.Sum(x => x.Total);

        var invoice = new EposProShopInvoiceEntity
        {
            PartitionKey = "Invoice",
            RowKey = InvoiceRowKey(invoiceId),
            CreatedAtUtc = createdAt,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Description = description,
            TotalAmount = totalAmount
        };

        var lines = new List<EposProShopInvoiceLineEntity>();
        foreach (var g in groups)
        {
            var line = new EposProShopInvoiceLineEntity
            {
                PartitionKey = InvoiceLinePartition(invoiceId),
                RowKey = InvoiceLineRowKey(g.ProductId),
                ProductId = g.ProductId.ToString("N"),
                ProductCode = g.ProductCode ?? string.Empty,
                ProductName = g.ProductName ?? string.Empty,
                Quantity = g.Quantity,
                RedemptionValuePerUnit = g.RedemptionValuePerUnit,
                TotalRedemptionValue = g.Total
            };
            lines.Add(line);
        }

        await _invoices.UpsertAsync(invoice, ct);
        await _invoiceLines.UpsertBatchAsync(lines, ct);

        foreach (var v in vouchers)
        {
            v.InvoiceId = invoiceId.ToString("N");
        }

        await _vouchers.UpsertBatchAsync(vouchers, ct);

        return (invoice, lines);
    }

    public Task<EposProShopInvoiceEntity?> GetInvoiceAsync(Guid invoiceId, CancellationToken ct)
    {
        return _invoices.GetAsync("Invoice", InvoiceRowKey(invoiceId), ct);
    }

    public async Task<List<EposProShopInvoiceLineEntity>> GetInvoiceLinesAsync(Guid invoiceId, CancellationToken ct)
    {
        var pk = InvoiceLinePartition(invoiceId);
        var list = new List<EposProShopInvoiceLineEntity>();
        await foreach (var line in _invoiceLines.QueryByPartitionAsync(pk, null, ct))
        {
            list.Add(line);
        }
        return list;
    }

    #endregion

    #region Reporting

    public async Task<List<EposAccountEntity>> GetAllAccountsAsync(CancellationToken ct)
    {
        var list = new List<EposAccountEntity>();
        await foreach (var a in _accounts.QueryByPartitionAsync("Account", null, ct))
        {
            list.Add(a);
        }
        return list;
    }

    public async Task<List<EposVoucherEntity>> GetAllVouchersAsync(CancellationToken ct)
    {
        var list = new List<EposVoucherEntity>();
        await foreach (var v in _vouchers.QueryByPartitionAsync("Voucher", null, ct))
        {
            list.Add(v);
        }
        return list;
    }

    #endregion
}
