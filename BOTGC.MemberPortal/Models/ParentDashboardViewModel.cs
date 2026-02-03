namespace BOTGC.MemberPortal.Models;

public sealed class ParentDashboardViewModel
{
    public string DisplayName { get; set; } = "Parent";
    public string? AvatarUrl { get; set; }

    public List<ParentChildViewModel> Children { get; set; } = new();

    public int? SelectedChildMemberNumber { get; set; }
    public string? SelectedChildName { get; set; }

    public List<QuickActionViewModel> ChildActions { get; set; } = new();
    public List<QuickActionViewModel> ParentActions { get; set; } = new();
}
