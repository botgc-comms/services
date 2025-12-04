using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Services;

public sealed class InMemoryVoucherService : IVoucherService
{
    private readonly List<VoucherViewModel> _vouchers;

    public InMemoryVoucherService()
    {
        _vouchers = new List<VoucherViewModel>
        {
            new VoucherViewModel
            {
                Id = 1,
                TypeKey = "practice",
                TypeTitle = "Practice vouchers",
                TypeDescription = "Use these for range or simulator practice.",
                Code = "JNR-PRAC-001",
                RemainingValue = 1m,
                Expiry = DateTimeOffset.UtcNow.AddMonths(1),
                IsUsed = false
            },
            new VoucherViewModel
            {
                Id = 2,
                TypeKey = "coaching",
                TypeTitle = "Coaching vouchers",
                TypeDescription = "Use these for junior coaching sessions.",
                Code = "JNR-COACH-001",
                RemainingValue = 1m,
                Expiry = DateTimeOffset.UtcNow.AddMonths(2),
                IsUsed = false
            },
            new VoucherViewModel
            {
                Id = 3,
                TypeKey = "practice",
                TypeTitle = "Practice vouchers",
                TypeDescription = "Use these for range or simulator practice.",
                Code = "JNR-PRAC-OLD",
                RemainingValue = 0m,
                Expiry = DateTimeOffset.UtcNow.AddDays(-10),
                IsUsed = true
            }
        };
    }

    public Task<IReadOnlyList<VoucherViewModel>> GetVouchersForMemberAsync(
        int memberId,
        CancellationToken cancellationToken = default)
    {
        // Later: filter by memberId
        var result = _vouchers.ToList();
        return Task.FromResult<IReadOnlyList<VoucherViewModel>>(result);
    }
}
