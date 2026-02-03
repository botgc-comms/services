namespace BOTGC.MemberPortal.Models;

public sealed class FilterBarViewModel
{
    public string AriaLabel { get; set; } = string.Empty;
    public string CurrentValue { get; set; } = string.Empty;
    public List<FilterBarOptionViewModel> Options { get; set; } = new();
}
