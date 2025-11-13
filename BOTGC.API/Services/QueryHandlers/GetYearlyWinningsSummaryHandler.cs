using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class GetYearlyWinningsSummaryHandler(
        ILogger<GetYearlyWinningsSummaryHandler> logger,
        ICompetitionPayoutStore payoutStore
    ) : QueryHandlerBase<GetYearlyWinningsSummaryQuery, YearlyWinningsSummaryDto>
{
    private readonly ILogger<GetYearlyWinningsSummaryHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ICompetitionPayoutStore _payoutStore = payoutStore ?? throw new ArgumentNullException(nameof(payoutStore));

    public override async Task<YearlyWinningsSummaryDto> Handle(GetYearlyWinningsSummaryQuery request, CancellationToken cancellationToken)
    {
        var headers = await _payoutStore.GetHeadersForYearAsync(request.Year, cancellationToken);
        if (headers.Count == 0)
        {
            return new YearlyWinningsSummaryDto
            {
                Year = request.Year,
                TotalPaid = 0m,
                CompetitionsCount = 0,
                OverallTop3 = Array.Empty<TopEarnerDto>(),
                Competitions = new List<CompetitionWinningsSummaryDto>()
            };
        }

        var latestByComp = headers
            .GroupBy(h => h.CompetitionId)
            .Select(g => g.OrderByDescending(h => h.CompetitionDate).First())
            .OrderBy(h => h.CompetitionDate)
            .ToList();

        var competitions = new List<CompetitionWinningsSummaryDto>();
        var overallAgg = new Dictionary<string, (string Name, decimal Amount, int Wins, int Placings)>(StringComparer.OrdinalIgnoreCase);
        var totalPaid = 0m;

        foreach (var h in latestByComp)
        {
            var compAgg = new Dictionary<string, (string Name, decimal Amount, int Wins, int Placings)>(StringComparer.OrdinalIgnoreCase);
            var divisions = new Dictionary<int, DivisionResultDto>();

            await foreach (var w in _payoutStore.GetWinnersByCompetitionAsync(h.CompetitionId, cancellationToken))
            {
                var amt = (decimal)w.Amount;
                var key = !string.IsNullOrWhiteSpace(w.CompetitorId) ? w.CompetitorId : $"NAME:{(w.CompetitorName ?? "Unknown").Trim()}";

                if (!overallAgg.TryGetValue(key, out var oa)) oa = (w.CompetitorName ?? string.Empty, 0m, 0, 0);
                oa.Amount += amt;
                if (w.Position == 1) oa.Wins += 1;
                oa.Placings += 1;
                overallAgg[key] = oa;

                if (!compAgg.TryGetValue(key, out var ca)) ca = (w.CompetitorName ?? string.Empty, 0m, 0, 0);
                ca.Amount += amt;
                if (w.Position == 1) ca.Wins += 1;
                ca.Placings += 1;
                compAgg[key] = ca;

                if (!divisions.TryGetValue(w.DivisionNumber, out var div))
                {
                    div = new DivisionResultDto
                    {
                        DivisionNumber = w.DivisionNumber,
                        DivisionName = string.IsNullOrWhiteSpace(w.DivisionName) ? $"Division {w.DivisionNumber}" : w.DivisionName!,
                        Placings = new List<PlacingDto>()
                    };
                    divisions[w.DivisionNumber] = div;
                }

                if (w.Position >= 1 && w.Position <= 5)
                {
                    div.Placings.Add(new PlacingDto
                    {
                        Position = w.Position,
                        CompetitorId = key,
                        CompetitorName = w.CompetitorName ?? string.Empty,
                        Amount = amt
                    });
                }

                totalPaid += amt;
            }

            var compTop = compAgg
                .Select(kv => new TopEarnerDto
                {
                    CompetitorId = kv.Key,
                    CompetitorName = kv.Value.Name,
                    TotalAmount = kv.Value.Amount,
                    Wins = kv.Value.Wins,
                    Placings = kv.Value.Placings
                })
                .OrderByDescending(t => t.TotalAmount)
                .ThenByDescending(t => t.Wins)
                .ThenBy(t => t.CompetitorName)
                .FirstOrDefault();

            var compDate = h.CompetitionDate.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(h.CompetitionDate, DateTimeKind.Utc)
                : h.CompetitionDate.ToUniversalTime();

            competitions.Add(new CompetitionWinningsSummaryDto
            {
                CompetitionId = h.CompetitionId,
                CompetitionName = h.CompetitionName ?? string.Empty,
                CompetitionDate = compDate,
                Entrants = h.Entrants,  
                PrizePot = (decimal)h.PrizePot,
                Revenue = (decimal)h.Revenue,
                Currency = h.Currency ?? "GBP",
                TopEarner = compTop,
                Divisions = divisions.Values
                    .Select(d => new DivisionResultDto
                    {
                        DivisionNumber = d.DivisionNumber,
                        DivisionName = d.DivisionName,
                        Placings = d.Placings.OrderBy(p => p.Position).ToList()
                    })
                    .OrderBy(d => d.DivisionNumber)
                    .ToList()
            });
        }

        var overallTop3 = overallAgg
            .Select(kv => new TopEarnerDto
            {
                CompetitorId = kv.Key,
                CompetitorName = kv.Value.Name,
                TotalAmount = kv.Value.Amount,
                Wins = kv.Value.Wins,
                Placings = kv.Value.Placings
            })
            .OrderByDescending(t => t.TotalAmount)
            .ThenByDescending(t => t.Wins)
            .ThenBy(t => t.CompetitorName)
            .Take(3)
            .ToArray();

        return new YearlyWinningsSummaryDto
        {
            Year = request.Year,
            TotalPaid = totalPaid,
            CompetitionsCount = competitions.Count,
            OverallTop3 = overallTop3,
            Competitions = competitions
        };
    }
}
