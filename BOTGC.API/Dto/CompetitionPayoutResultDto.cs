namespace BOTGC.API.Dto
{
    public sealed class CompetitionPayoutResultDto
    {
        public int CompetitionId { get; set; }
        public string CompetitionName { get; set; } = string.Empty;
        public DateTime CompetitionDate { get; set; }
        public int Entrants { get; set; }
        public decimal EntryFee { get; set; }
        public int Divisions { get; set; }
        public decimal PayoutPercent { get; set; }
        public decimal Revenue { get; set; }
        public decimal PrizePot { get; set; }
        public decimal CharityAmount { get; set; }
        public decimal ClubIncome { get; set; }
        public List<WinnerPayoutDto> Winners { get; set; } = new();
        public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;
        public string RuleSet { get; set; } = "default";
        public string Currency { get; set; } = "GBP";
    }

    public sealed class WinnerPayoutDto
    {
        public int CompetitionId { get; set; }
        public int DivisionNumber { get; set; }
        public string DivisionName { get; set; } = string.Empty;
        public int Position { get; set; }
        public string CompetitorId { get; set; } = string.Empty;
        public string CompetitorName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "GBP";
    }

    public sealed class PrizeRule
    {
        public string Name { get; }
        public IReadOnlyList<decimal> Splits { get; }
        public bool ResidualToLast { get; }

        public PrizeRule(string name, IEnumerable<decimal> splits, bool residualToLast = true)
        {
            var list = splits.ToList();
            if (list.Count == 0) throw new ArgumentException("At least one split is required.");
            var total = list.Sum();
            Splits = list.Select(s => total == 0 ? 0 : s / total).ToArray();
            Name = name;
            ResidualToLast = residualToLast;
        }

        public static PrizeRule IndividualTop3 => new("IndividualTop3", new[] { 0.50m, 0.33m, 0.17m }, true);
        public static PrizeRule PairsOrTeamTop5 => new("PairsOrTeamTop5", new[] { 0.30m, 0.25m, 0.20m, 0.15m, 0.10m }, true);
    }

    public sealed class DivisionWinnersInput
    {
        public int DivisionNumber { get; set; }
        public string DivisionName { get; set; } = string.Empty;
        public List<(string CompetitorId, string CompetitorName)> OrderedByPosition { get; set; } = new();
    }
    public sealed class CompetitionPayoutInput
    {
        public int CompetitionId { get; set; }
        public string CompetitionName { get; set; } = string.Empty;
        public DateTime CompetitionDate { get; set; }
        public int Entrants { get; set; }
        public decimal EntryFee { get; set; }
        public int Divisions { get; set; }
        public decimal PayoutPercent { get; set; }
        public decimal CharityAmount { get; set; }
        public string Currency { get; set; } = "GBP";
        public string RuleSet { get; set; } = "default";
        public List<DivisionWinnersInput> WinnersPerDivision { get; set; } = new();
    }

}
