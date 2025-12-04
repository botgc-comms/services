using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Services.TileAdapters;

public sealed class JuniorMentorTileAdapter : ITileAdapter
{
    public string Type => "junior_mentor";

    public Task<DashboardTileViewModel?> BuildTileAsync(
        TileDefinition definition,
        MemberContext member,
        CancellationToken cancellationToken = default)
    {
        var title = string.IsNullOrWhiteSpace(definition.Name)
            ? "Contact your junior mentor"
            : definition.Name;

        var description = $"Send a message to your mentor about your golf or your category ({member.JuniorCategory}).";

        var tile = new DashboardTileViewModel
        {
            Type = Type,
            Title = title,
            Description = description,
            Controller = "Mentor",
            Action = "Contact",
            ImageUrl = "/img/buddy.jpg",
            BackgroundColor = definition.Color
        };

        return Task.FromResult<DashboardTileViewModel?>(tile);
    }
}