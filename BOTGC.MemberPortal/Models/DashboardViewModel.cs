namespace BOTGC.MemberPortal.Models;

public sealed class DashboardViewModel
{
    public string DisplayName { get; init; } = string.Empty;
    public string JuniorCategory { get; init; } = string.Empty;

    public IReadOnlyList<DashboardTileViewModel> Tiles { get; init; } =
        new List<DashboardTileViewModel>();
}