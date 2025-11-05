using BOTGC.API.Dto;
using BOTGC.API.Interfaces;

namespace BOTGC.API.Common;

public sealed class DefaultCompetitionWinnersUrlProvider : ICompetitionWinnersUrlProvider
{
    public string? TryGetUrl(CompetitionWinningsSummaryDto competition)
    {
        if (competition == null) return null;
        return competition.CompetitionId > 0 ? $"/competitions/{competition.CompetitionId}/winners" : null;
    }
}