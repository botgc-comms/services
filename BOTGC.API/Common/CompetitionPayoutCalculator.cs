using BOTGC.API.Dto;
using BOTGC.API.Interfaces;

namespace BOTGC.API.Common
{
    public sealed class CompetitionPayoutCalculator : ICompetitionPayoutCalculator
    {
        public CompetitionPayoutResultDto Compute(CompetitionPayoutInput input, PrizeRule rule, int maxPlacesPerDivision)
        {
            var payoutPercent = input.PayoutPercent > 1m ? input.PayoutPercent / 100m : input.PayoutPercent;
            var revenue = input.Entrants * input.EntryFee;
            var prizePot = Round2(revenue * payoutPercent);
            var divisions = Math.Max(1, input.Divisions);
            var perDivisionFund = prizePot / divisions;

            var winners = new List<WinnerPayoutDto>();

            foreach (var div in input.WinnersPerDivision.OrderBy(d => d.DivisionNumber))
            {
                var placesToPay = Math.Min(maxPlacesPerDivision, rule.Splits.Count);
                placesToPay = Math.Min(placesToPay, div.OrderedByPosition.Count);
                if (placesToPay <= 0) continue;

                var amounts = Allocate(perDivisionFund, rule.Splits.Take(placesToPay).ToArray(), rule.ResidualToLast);

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
                PrizePot = Round2(prizePot),
                CharityAmount = Round2(input.CharityAmount),
                ClubIncome = clubIncome,
                Winners = winners,
                RuleSet = input.RuleSet,
                Currency = input.Currency,
                CalculatedAtUtc = DateTime.UtcNow
            };
        }

        private static decimal[] Allocate(decimal fund, IReadOnlyList<decimal> normalisedSplits, bool residualToLast)
        {
            var amounts = new decimal[normalisedSplits.Count];
            var runningTotal = 0m;

            for (var i = 0; i < normalisedSplits.Count; i++)
            {
                if (i == normalisedSplits.Count - 1 && residualToLast)
                {
                    amounts[i] = Round2(fund - runningTotal);
                }
                else
                {
                    var portion = Round2(fund * normalisedSplits[i]);
                    amounts[i] = portion;
                    runningTotal += portion;
                }
            }

            return amounts;
        }

        private static decimal Round2(decimal v) => Math.Round(v, 2, MidpointRounding.AwayFromZero);
    }
}
