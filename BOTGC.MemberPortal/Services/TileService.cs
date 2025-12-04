using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Services;

public sealed class TileService : ITileService
{
    private readonly AppSettings _settings;
    private readonly IReadOnlyDictionary<string, ITileAdapter> _adapters;

    public TileService(
        AppSettings settings,
        IEnumerable<ITileAdapter> adapters)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));

        _adapters = adapters
            .GroupBy(a => a.Type, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g => g.First(),
                StringComparer.OrdinalIgnoreCase);
    }

    public async Task<IReadOnlyList<DashboardTileViewModel>> GetTilesForMemberAsync(
        MemberContext member,
        CancellationToken cancellationToken = default)
    {
        if (member is null)
        {
            throw new ArgumentNullException(nameof(member));
        }

        var category = member.JuniorCategory ?? string.Empty;

        var applicableDefinitions = _settings.Tiles
            .Where(t => t.Categories.Any(c =>
                string.Equals(c, category, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(t => t.SortOrder)
            .ToList();

        var results = new List<DashboardTileViewModel>();

        foreach (var definition in applicableDefinitions)
        {
            if (!_adapters.TryGetValue(definition.Type, out var adapter))
            {
                continue;
            }

            var tile = await adapter.BuildTileAsync(definition, member, cancellationToken);
            if (tile != null)
            {
                results.Add(tile);
            }
        }

        return results;
    }
}