using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Services.TileAdapters;

public sealed class RulesQuizTileAdapter : ITileAdapter
{
    public string Type => "rules_quiz";

    public Task<DashboardTileViewModel?> BuildTileAsync(
        TileDefinition definition,
        MemberContext member,
        CancellationToken cancellationToken = default)
    {
        var tile = new DashboardTileViewModel
        {
            Type = Type,
            Title = string.IsNullOrWhiteSpace(definition.Name)
                ? "Take a rules quiz"
                : definition.Name,
            Description = "Test your golf rules knowledge in a quick quiz.",
            Controller = "Rules",
            Action = "Quiz",
            BackgroundColor = definition.Color
        };

        return Task.FromResult<DashboardTileViewModel?>(tile);
    }
}