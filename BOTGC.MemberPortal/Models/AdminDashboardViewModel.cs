namespace BOTGC.MemberPortal.Models;

public sealed class AdminDashboardViewModel
{
    public string DisplayName { get; init; } = "Admin";

    public string? AvatarUrl { get; init; }

    public IReadOnlyList<QuickActionViewModel> AdminActions { get; init; } = Array.Empty<QuickActionViewModel>();
}