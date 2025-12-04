using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Services.TileAdapters;

public sealed class HandicapSessionTileAdapter : ITileAdapter
{
    public string Type => "handicap_session";

    public Task<DashboardTileViewModel?> BuildTileAsync(
        TileDefinition definition,
        MemberContext member,
        CancellationToken cancellationToken = default)
    {
        var tile = new DashboardTileViewModel
        {
            Type = Type,
            Title = string.IsNullOrWhiteSpace(definition.Name)
                ? "Next handicap session"
                : definition.Name,
            Description = "Find out when your next handicap coaching session is.",
            Controller = "Handicap",
            Action = "NextSession",
            BackgroundColor = definition.Color
        };

        return Task.FromResult<DashboardTileViewModel?>(tile);
    }
}