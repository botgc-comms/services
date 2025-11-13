using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;
using static BOTGC.API.Models.BottleCalibrationEntity;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class GetCompetitionPayoutDetailsHandler(
        ILogger<GetCompetitionPayoutDetailsHandler> logger,
        ICompetitionPayoutStore payoutStore
    ) : QueryHandlerBase<GetCompetitionPayoutDetailsQuery, CompetitionWinningsSummaryDto?>
{
    private readonly ILogger<GetCompetitionPayoutDetailsHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly ICompetitionPayoutStore _payoutStore = payoutStore ?? throw new ArgumentNullException(nameof(payoutStore));

    public async override Task<CompetitionWinningsSummaryDto?> Handle(GetCompetitionPayoutDetailsQuery request, CancellationToken cancellationToken)
    {
        if (request.CompetitionId <= 0)
        {
            throw new ArgumentException("CompetitionId must be a positive integer.", nameof(request.CompetitionId));
        }

        var header = await FindHeaderAsync(request.CompetitionId, cancellationToken);
        if (header == null)
        {
            _logger.LogWarning("No payout header found for CompetitionId {CompetitionId}.", request.CompetitionId);
            return null;
        }

        var winners = new List<CompetitionPayoutWinnerEntity>();
        await foreach (var w in _payoutStore.GetWinnersByCompetitionAsync(request.CompetitionId, cancellationToken))
        {
            winners.Add(w);
        }

        if (winners.Count == 0)
        {
            _logger.LogWarning("No payout winners found for CompetitionId {CompetitionId}.", request.CompetitionId);
        }

        var compDate = header.CompetitionDate.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(header.CompetitionDate, DateTimeKind.Utc)
            : header.CompetitionDate.ToUniversalTime();

        var divisions = new Dictionary<int, DivisionResultDto>();
        var earnerAgg = new Dictionary<string, (string Name, decimal Amount, int Wins, int Placings)>(StringComparer.OrdinalIgnoreCase);

        foreach (var w in winners.OrderBy(x => x.DivisionNumber).ThenBy(x => x.Position))
        {
            var amount = (decimal)w.Amount;
            var key = !string.IsNullOrWhiteSpace(w.CompetitorId)
                ? w.CompetitorId
                : $"NAME:{(w.CompetitorName ?? "Unknown").Trim()}";

            if (!divisions.TryGetValue(w.DivisionNumber, out var div))
            {
                div = new DivisionResultDto
                {
                    DivisionNumber = w.DivisionNumber,
                    DivisionName = string.IsNullOrWhiteSpace(w.DivisionName)
                        ? $"Division {w.DivisionNumber}"
                        : w.DivisionName,
                    Placings = new List<PlacingDto>()
                };
                divisions[w.DivisionNumber] = div;
            }

            div.Placings.Add(new PlacingDto
            {
                Position = w.Position,
                CompetitorId = key,
                CompetitorName = w.CompetitorName ?? string.Empty,
                Amount = amount
            });

            if (!earnerAgg.TryGetValue(key, out var agg))
            {
                agg = (w.CompetitorName ?? string.Empty, 0m, 0, 0);
            }

            agg.Amount += amount;
            if (w.Position == 1) agg.Wins += 1;
            agg.Placings += 1;
            earnerAgg[key] = agg;
        }

        TopEarnerDto? topEarner = null;
        if (earnerAgg.Count > 0)
        {
            topEarner = earnerAgg
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
        }

        var dto = new CompetitionWinningsSummaryDto
        {
            CompetitionId = header.CompetitionId,
            CompetitionName = header.CompetitionName ?? string.Empty,
            CompetitionDate = compDate,
            Entrants = header.Entrants,
            PrizePot = (decimal)header.PrizePot,
            Revenue = (decimal)header.Revenue,
            Currency = header.Currency ?? "GBP",
            TopEarner = topEarner,
            Divisions = divisions.Values
                .Select(d => new DivisionResultDto
                {
                    DivisionNumber = d.DivisionNumber,
                    DivisionName = d.DivisionName,
                    Placings = d.Placings
                        .OrderBy(p => p.Position)
                        .ThenBy(p => p.CompetitorName)
                        .ToList()
                })
                .OrderBy(d => d.DivisionNumber)
                .ToList()
        };

        return dto;
    }

    private async Task<CompetitionPayoutHeaderEntity?> FindHeaderAsync(int competitionId, CancellationToken ct)
    {
        var currentYear = DateTime.UtcNow.Year;
        var oldestYear = currentYear - 5;

        for (var year = currentYear; year >= oldestYear; year--)
        {
            var headers = await _payoutStore.GetHeadersForYearAsync(year, ct);
            var match = headers.FirstOrDefault(h => h.CompetitionId == competitionId);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }
}
