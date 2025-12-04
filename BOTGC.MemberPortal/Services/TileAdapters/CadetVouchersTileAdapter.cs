using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Services.TileAdapters;

public sealed class CadetVouchersTileAdapter : ITileAdapter
{
    public string Type => "cadet_vouchers";

    public Task<DashboardTileViewModel?> BuildTileAsync(
        TileDefinition definition,
        MemberContext member,
        CancellationToken cancellationToken = default)
    {
        var tile = new DashboardTileViewModel
        {
            Type = Type,
            Title = string.IsNullOrWhiteSpace(definition.Name)
                ? "Your vouchers"
                : definition.Name,
            Description = "See and use your practice and coaching vouchers.",
            Controller = "Vouchers",
            Action = "Index",
            BackgroundColor = definition.Color
        };

        return Task.FromResult<DashboardTileViewModel?>(tile);
    }
}
