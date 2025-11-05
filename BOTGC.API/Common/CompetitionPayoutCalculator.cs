using BOTGC.API.Dto;
using BOTGC.API.Interfaces;

namespace BOTGC.API.Common
{
    public sealed class CompetitionPayoutCalculator : ICompetitionPayoutCalculator
    {
        public CompetitionPayoutResultDto Compute(CompetitionPayoutInput input, PrizeRule rule, int maxPlacesPerDivision)
        {
            var payoutPercent = input.PayoutPercent > 1m ? input.PayoutPercent / 100m : input.PayoutPercent;
            var revenue = Round2(input.Entrants * input.EntryFee);
            var prizePot = Round2(revenue * payoutPercent);
            var divisions = Math.Max(1, input.Divisions);
            var perDivisionFund = prizePot / divisions;

            var winners = new List<WinnerPayoutDto>();

            foreach (var div in input.WinnersPerDivision.OrderBy(d => d.DivisionNumber))
            {
                var placesToPay = Math.Min(Math.Min(maxPlacesPerDivision, rule.Splits.Count), div.OrderedByPosition.Count);
                if (placesToPay <= 0) continue;

                var splits = NormaliseAndFill(rule.Splits.Take(placesToPay).ToList());
                var amounts = Allocate(perDivisionFund, splits, residualToLast: true);
                RoundAndBalanceToPounds(amounts, perDivisionFund);

                for (var i = 0; i < placesToPay; i++)
                {
                    var (id, name) = div.OrderedByPosition[i];
                    winners.Add(new WinnerPayoutDto
                    {
                        CompetitionId = input.CompetitionId,
                        DivisionNumber = div.DivisionNumber,
                        DivisionName = string.IsNullOrWhiteSpace(div.DivisionName) ? $"Division {div.DivisionNumber}" : div.DivisionName,
                        Position = i + 1,
                        CompetitorId = id,
                        CompetitorName = name,
                        Amount = amounts[i],
                        Currency = input.Currency
                    });
                }
            }

            var clubIncome = Round2(revenue - prizePot - input.CharityAmount);

            return new CompetitionPayoutResultDto
            {
                CompetitionId = input.CompetitionId,
                CompetitionName = input.CompetitionName,
                CompetitionDate = input.CompetitionDate,
                Entrants = input.Entrants,
                EntryFee = input.EntryFee,
                Divisions = divisions,
                PayoutPercent = payoutPercent,
                Revenue = revenue,
                PrizePot = prizePot,
                CharityAmount = Round2(input.CharityAmount),
                ClubIncome = clubIncome,
                Winners = winners,
                RuleSet = input.RuleSet,
                Currency = input.Currency,
                CalculatedAtUtc = DateTime.UtcNow
            };
        }

        private static List<decimal> NormaliseAndFill(IReadOnlyList<decimal> splits)
        {
            var list = splits?.ToList() ?? new List<decimal>();
            if (list.Count == 0) return new List<decimal> { 1m };

            for (int i = 0; i < list.Count; i++) if (list[i] < 0m) list[i] = 0m;

            var sum = list.Sum();
            if (sum <= 0m)
            {
                list[0] = 1m;
                for (int i = 1; i < list.Count; i++) list[i] = 0m;
                return list;
            }

            if (sum < 1m) list[^1] += (1m - sum);
            else if (sum > 1m) for (int i = 0; i < list.Count; i++) list[i] = list[i] / sum;

            return list;
        }

        private static decimal[] Allocate(decimal fund, IReadOnlyList<decimal> normalisedSplits, bool residualToLast)
        {
            var amounts = new decimal[normalisedSplits.Count];
            var running = 0m;

            for (var i = 0; i < normalisedSplits.Count; i++)
            {
                if (i == normalisedSplits.Count - 1 && residualToLast)
                {
                    amounts[i] = Round2(fund - running);
                }
                else
                {
                    var portion = Round2(fund * normalisedSplits[i]);
                    amounts[i] = portion;
                    running += portion;
                }
            }

            return amounts;
        }

        private static void RoundAndBalanceToPounds(decimal[] amounts, decimal divisionFund)
        {
            var target = Round0(divisionFund);
            var rounded = new decimal[amounts.Length];

            for (int i = 0; i < amounts.Length; i++) rounded[i] = Round0(amounts[i]);

            var delta = target - rounded.Sum();
            if (delta != 0m) rounded[^1] += delta;

            for (int i = 0; i < amounts.Length; i++) amounts[i] = rounded[i];
        }

        private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
        private static decimal Round0(decimal v) => Math.Round(v, 0, MidpointRounding.AwayFromZero);
    }
}
