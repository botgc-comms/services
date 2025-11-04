using BOTGC.API.Dto;

namespace BOTGC.API.Interfaces
{
    public interface ICompetitionPayoutCalculator
    {
        CompetitionPayoutResultDto Compute(CompetitionPayoutInput input, PrizeRule rule, int maxPlacesPerDivision);
    }
}