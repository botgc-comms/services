namespace BOTGC.MemberPortal.Models;

public sealed class VoucherViewModel
{
    public int Id { get; init; }
    public string TypeKey { get; init; } = string.Empty;
    public string TypeTitle { get; init; } = string.Empty;
    public string TypeDescription { get; init; } = string.Empty;
    public string Image { get; set; } = string.Empty;

    public string Code { get; init; } = string.Empty;
    public decimal RemainingValue { get; init; }
    public DateTimeOffset? Expiry { get; init; }
    public bool IsUsed { get; init; }

    public bool IsActive =>
        !IsUsed && (!Expiry.HasValue || Expiry.Value >= DateTimeOffset.UtcNow);

    public string StatusLabel
    {
        get
        {
            if (IsUsed)
            {
                return "Used";
            }

            if (Expiry.HasValue && Expiry.Value < DateTimeOffset.UtcNow)
            {
                return "Expired";
            }

            return "Active";
        }
    }
}
