using BOTGC.API.Interfaces;

namespace BOTGC.API.Services;

public sealed class CompetitionPrizeConfigProvider : IPrizeConfigProvider
{
    private readonly List<PrizeRuleSet> _sets;

    public CompetitionPrizeConfigProvider(IConfiguration configuration)
    {
        var opts = new PrizePayoutOptions();
        configuration.GetSection("PrizePayout").Bind(opts);
        _sets = (opts.RuleSets ?? new()).OrderBy(s => s.DateFrom).ToList();
    }

    public PrizeRuleSet GetEffective(DateTime competitionDate)
    {
        var date = competitionDate.Date;
        var pick = _sets.Where(s => s.DateFrom.Date <= date)
                        .OrderByDescending(s => s.DateFrom)
                        .FirstOrDefault();

        if (pick != null)
        {
            pick.Splits ??= new PrizeSplits();
            if (pick.Splits.IndividualTop3 == null || pick.Splits.IndividualTop3.Count == 0)
                pick.Splits.IndividualTop3 = new() { 0.50m, 0.33m, 0.17m };
            if (pick.Splits.PairsTeamTop5 == null || pick.Splits.PairsTeamTop5.Count == 0)
                pick.Splits.PairsTeamTop5 = new() { 0.30m, 0.25m, 0.20m, 0.15m, 0.10m };
            return pick;
        }

        return new PrizeRuleSet
        {
            DateFrom = DateTime.MinValue,
            PayoutPercent = 0.50m,
            EntryFeeFallback = 4.00m,
            Charity = new CharityRuleConfig
            {
                Enabled = true,
                ExemptWhenDivisionsEquals = 4,
                BaselineEntryFee = 4.00m
            },
            Splits = new PrizeSplits
            {
                IndividualTop3 = new() { 0.50m, 0.33m, 0.17m },
                PairsTeamTop5 = new() { 0.30m, 0.25m, 0.20m, 0.15m, 0.10m }
            }
        };
    }


    public decimal ComputeCharityAmount(PrizeRuleSet cfg, int divisions, int entrants, decimal entryFee, decimal revenue)
    {
        if (!cfg.Charity.Enabled) return 0m;
        if (cfg.Charity.ExemptWhenDivisionsEquals.HasValue && divisions == cfg.Charity.ExemptWhenDivisionsEquals.Value) return 0m;
        if (cfg.Charity.FixedAmount.HasValue) return Math.Round(cfg.Charity.FixedAmount.Value, 2, MidpointRounding.AwayFromZero);
        if (cfg.Charity.PercentOfRevenue.HasValue) return Math.Round(revenue * cfg.Charity.PercentOfRevenue.Value, 2, MidpointRounding.AwayFromZero);

        if (cfg.Charity.BaselineEntryFee.HasValue)
        {
            var delta = entryFee - cfg.Charity.BaselineEntryFee.Value;
            return delta > 0 ? Math.Round(entrants * delta, 2, MidpointRounding.AwayFromZero) : 0m;
        }

        return 0m;
    }

    public (string Name, IReadOnlyList<decimal> Splits) GetSplitForFormat(PrizeRuleSet cfg, string? competitionFormat)
    {
        var f = (competitionFormat ?? string.Empty).ToLowerInvariant();
        var isPairsTeam = f.Contains("foursomes") || f.Contains("greensomes") || f.Contains("scramble") || f.Contains("bbb") || f.Contains("4bbb") || f.Contains("pairs") || f.Contains("team");

        if (isPairsTeam)
        {
            return ("PairsOrTeamTop5", cfg.Splits.PairsTeamTop5);
        }

        return ("IndividualTop3", cfg.Splits.IndividualTop3);
    }
}
