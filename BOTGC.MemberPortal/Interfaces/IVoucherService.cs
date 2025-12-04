using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Interfaces;

public interface IVoucherService
{
    Task<IReadOnlyList<VoucherViewModel>> GetVouchersForMemberAsync(
        int memberId,
        CancellationToken cancellationToken = default);
}