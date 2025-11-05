using BOTGC.API.Dto;

namespace BOTGC.API.Interfaces;

public interface ICompetitionWinnersUrlProvider
{
    string? TryGetUrl(CompetitionWinningsSummaryDto competition);
}
