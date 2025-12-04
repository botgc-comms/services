using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Services.TileAdapters;

public sealed class CategoryProgressTileAdapter : ITileAdapter
{
    public string Type => "category_progress";

    public Task<DashboardTileViewModel?> BuildTileAsync(
        TileDefinition definition,
        MemberContext member,
        CancellationToken cancellationToken = default)
    {
        var progressPercent = 65;

        var tile = new DashboardTileViewModel
        {
            Type = Type,
            Title = string.IsNullOrWhiteSpace(definition.Name)
                ? "Next membership category"
                : definition.Name,
            Description = $"You are {progressPercent}% of the way towards your next category.",
            ProgressPercent = progressPercent,
            ProgressLabel = $"{progressPercent}% complete",
            BackgroundColor = definition.Color
        };

        return Task.FromResult<DashboardTileViewModel?>(tile);
    }
}