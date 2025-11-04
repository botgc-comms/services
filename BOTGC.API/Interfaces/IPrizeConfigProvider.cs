namespace BOTGC.API.Interfaces
{
    public interface IPrizeConfigProvider
    {
        PrizeRuleSet GetEffective(DateTime competitionDate);
        decimal ComputeCharityAmount(PrizeRuleSet cfg, int divisions, int entrants, decimal entryFee, decimal revenue);
        (string Name, IReadOnlyList<decimal> Splits) GetSplitForFormat(PrizeRuleSet cfg, string? competitionFormat);
    }
}