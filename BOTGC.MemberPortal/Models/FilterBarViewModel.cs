namespace BOTGC.MemberPortal.Models;

public sealed class FilterBarViewModel
{
    public string AriaLabel { get; set; } = "Filters";

    public string CurrentValue { get; set; } = "all";

    public List<FilterBarOptionViewModel> Options { get; set; } = new();
}
