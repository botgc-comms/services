using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BOTGC.API.Models;

namespace BOTGC.API.Interfaces;

public interface IEposStore
{
    // Accounts
    Task<EposAccountEntity?> GetAccountAsync(int memberId, CancellationToken ct);
    Task<EposAccountEntity> CreateOrGetAccountAsync(int memberId, string displayName, CancellationToken ct);
    Task UpsertAccountAsync(EposAccountEntity account, CancellationToken ct);

    // Ledger / Balance
    Task<EposAccountTransactionEntity> CreditAccountAsync(EposAccountEntity account, decimal amount, string reason, CancellationToken ct);
    Task<EposAccountTransactionEntity> DebitAccountAsync(EposAccountEntity account, decimal amount, string reason, Guid? voucherId, Guid? productId, CancellationToken ct);
    Task<IReadOnlyList<EposAccountTransactionEntity>> GetAccountTransactionsAsync(int memberId, DateTimeOffset? fromUtc, DateTimeOffset? toUtc, CancellationToken ct);
    Task<decimal> GetBalanceAsync(int memberId, CancellationToken ct);

    // Products
    Task<EposProductEntity?> GetProductAsync(Guid productId, CancellationToken ct);
    Task<List<EposProductEntity>> GetAllProductsAsync(CancellationToken ct);
    Task<EposProductEntity> UpsertProductAsync(EposProductEntity product, CancellationToken ct);

    // Vouchers
    Task<List<EposVoucherEntity>> GetVouchersForAccountAsync(int memberId, CancellationToken ct);
    Task<EposVoucherEntity?> GetVoucherAsync(Guid voucherId, CancellationToken ct);
    Task<(EposVoucherEntity? Voucher, EposAccountEntity? Account)> GetVoucherByCodeAsync(string code, CancellationToken ct);

    Task<EposVoucherEntity> CreateVoucherAsync(
        int memberId,
        EposProductEntity product,
        bool isBonus,
        string? awardReason,
        string voucherCode,
        DateTimeOffset issuedAtUtc,
        DateTimeOffset? expiresAtUtc,
        CancellationToken ct);

    Task<EposVoucherEntity?> RedeemVoucherAsync(
        EposVoucherEntity voucher,
        EposAccountEntity account,
        DateTimeOffset redeemedAtUtc,
        CancellationToken ct);

    Task<List<EposVoucherEntity>> GetRedeemedUninvoicedVouchersAsync(
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        CancellationToken ct);

    // Invoices
    Task<(EposProShopInvoiceEntity Invoice, List<EposProShopInvoiceLineEntity> Lines)> CreateInvoiceFromVouchersAsync(
        DateTimeOffset? fromUtc,
        DateTimeOffset? toUtc,
        string? description,
        IReadOnlyList<EposVoucherEntity> vouchers,
        CancellationToken ct);

    Task<EposProShopInvoiceEntity?> GetInvoiceAsync(Guid invoiceId, CancellationToken ct);
    Task<List<EposProShopInvoiceLineEntity>> GetInvoiceLinesAsync(Guid invoiceId, CancellationToken ct);

    // Reporting helpers
    Task<List<EposAccountEntity>> GetAllAccountsAsync(CancellationToken ct);
    Task<List<EposVoucherEntity>> GetAllVouchersAsync(CancellationToken ct);
}
