using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Services;

public sealed class ApiVoucherService : IVoucherService
{
    private readonly HttpClient _client;

    public ApiVoucherService(IHttpClientFactory httpClientFactory)
    {
        _client = httpClientFactory.CreateClient("Api");
    }

    public async Task<IReadOnlyList<VoucherViewModel>> GetVouchersForMemberAsync(
        int memberId,
        CancellationToken cancellationToken = default)
    {
        var response = await _client.GetAsync($"api/epos/accounts/{memberId}/entitlements", cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return Array.Empty<VoucherViewModel>();
        }

        response.EnsureSuccessStatusCode();

        var entitlements = await response.Content.ReadFromJsonAsync<AccountEntitlementsDto>(cancellationToken: cancellationToken);

        if (entitlements is null || entitlements.Products is null || entitlements.Products.Count == 0)
        {
            return Array.Empty<VoucherViewModel>();
        }

        var result = new List<VoucherViewModel>(entitlements.Products.Count);
        var id = 1;

        foreach (var product in entitlements.Products)
        {
            var vm = new VoucherViewModel
            {
                Id = id++,
                TypeKey = product.ProductCode,
                TypeTitle = product.ProductName,
                TypeDescription = product.ProductName,
                Image = product.ProductImage,
                
                Code = product.VoucherId.HasValue
                    ? product.VoucherId.Value.ToString("N")
                    : product.ProductId.ToString("N"),
                RemainingValue = product.IsAvailable ? 1m : 0m,
                IsUsed = !product.IsAvailable
            };

            result.Add(vm);
        }

        return result;
    }

    private sealed class AccountEntitlementsDto
    {
        public int MemberId { get; set; }
        public string DisplayName { get; set; } = string.Empty;
        public decimal CurrentBalance { get; set; }
        public List<ProductEntitlementDto> Products { get; set; } = new();
    }

    private sealed class ProductEntitlementDto
    {
        public Guid ProductId { get; set; }
        public Guid? VoucherId { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string ProductImage { get; set; } = string.Empty;
        public decimal RedemptionValue { get; set; }
        public decimal AllowanceCharge { get; set; }
        public bool IsAvailable { get; set; }
    }
}