// File: API/Services/EposMapping.cs
using System;
using BOTGC.API.Dto;
using BOTGC.API.Models;

namespace BOTGC.API.Services
{
    public static class EposMapping
    {
        public static AccountVoucherDto ToAccountVoucherDto(this EposVoucherEntity voucher)
        {
            if (voucher == null) throw new ArgumentNullException(nameof(voucher));

            return new AccountVoucherDto
            {
                VoucherId = voucher.VoucherId,
                MemberId = voucher.MemberId,
                VoucherCode = voucher.VoucherCode,
                ProductId = voucher.ProductIdGuid,
                ProductCode = voucher.ProductCode,
                ProductName = voucher.ProductName,
                RedemptionValue = voucher.RedemptionValue,
                AllowanceCharge = voucher.AllowanceCharge,
                IsBonus = voucher.IsBonus,
                AwardReason = voucher.AwardReason,
                Status = voucher.Status,
                IssuedAtUtc = voucher.IssuedAtUtc,
                RedeemedAtUtc = voucher.RedeemedAtUtc,
                ExpiresAtUtc = voucher.ExpiresAtUtc,
                InvoiceId = voucher.InvoiceId
            };
        }

        public static ProductDto ToProductDto(this EposProductEntity product)
        {
            return new ProductDto
            {
                ProductId = product.ProductId,
                Code = product.Code,
                DisplayName = product.DisplayName,
                Description = product.Description,
                Category = product.Category,
                DefaultRedemptionValue = product.DefaultRedemptionValue,
                DefaultAllowanceCharge = product.DefaultAllowanceCharge,
                IsActive = product.IsActive,
                ProductExpiresAtUtc = product.ProductExpiresAtUtc
            };
        }

        public static ProShopInvoiceDetailDto ToProShopInvoiceDetailDto(this EposProShopInvoiceEntity invoice)
        {
            if (invoice == null) throw new ArgumentNullException(nameof(invoice));

            return new ProShopInvoiceDetailDto
            {
                InvoiceId = invoice.InvoiceId,
                CreatedAtUtc = invoice.CreatedAtUtc,
                FromUtc = invoice.FromUtc,
                ToUtc = invoice.ToUtc,
                Description = invoice.Description,
                TotalAmount = invoice.TotalAmount
            };
        }

        public static ProShopInvoiceLineDto ToProShopInvoiceLineDto(this EposProShopInvoiceLineEntity line)
        {
            if (line == null) throw new ArgumentNullException(nameof(line));

            return new ProShopInvoiceLineDto
            {
                InvoiceId = line.InvoiceId,
                ProductId = line.ProductIdGuid,
                ProductCode = line.ProductCode,
                ProductName = line.ProductName,
                Quantity = line.Quantity,
                RedemptionValuePerUnit = line.RedemptionValuePerUnit,
                TotalRedemptionValue = line.TotalRedemptionValue
            };
        }
    }
}
