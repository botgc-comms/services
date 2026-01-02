namespace BOTGC.MemberPortal.Models;

public sealed class VouchersPageViewModel
{
    public IReadOnlyList<VoucherViewModel> Active { get; init; } = new List<VoucherViewModel>();
    public IReadOnlyList<VoucherViewModel> Used { get; init; } = new List<VoucherViewModel>();
    public IReadOnlyList<VoucherViewModel> Expired { get; init; } = new List<VoucherViewModel>();
}