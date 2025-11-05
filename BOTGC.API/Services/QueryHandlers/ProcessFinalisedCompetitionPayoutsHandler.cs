using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;
using System.Threading;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class ProcessFinalisedCompetitionPayoutsHandler(
        IOptions<AppSettings> settings,
        ILogger<ProcessFinalisedCompetitionPayoutsHandler> logger,
        IMediator mediator,
        ICompetitionPayoutCalculator payoutCalculator,
        ICompetitionPayoutStore payoutStore,
        IPrizeConfigProvider prizeConfig
    ) : QueryHandlerBase<ProcessFinalisedCompetitionPayoutsCommand, int>
{
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<ProcessFinalisedCompetitionPayoutsHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ICompetitionPayoutCalculator _payoutCalculator = payoutCalculator ?? throw new ArgumentNullException(nameof(payoutCalculator));
    private readonly ICompetitionPayoutStore _payoutStore = payoutStore ?? throw new ArgumentNullException(nameof(payoutStore));
    private readonly IPrizeConfigProvider _prizeConfig = prizeConfig ?? throw new ArgumentNullException(nameof(prizeConfig));

    public override async Task<int> Handle(ProcessFinalisedCompetitionPayoutsCommand request, CancellationToken cancellationToken)
    {
        var eligibilityExpression = new Regex(_settings.PrizePayout.EligibilityExpression ?? ".*", RegexOptions.IgnoreCase);    

        var competitions = await _mediator.Send(new GetActiveAndFutureCompetitionsQuery(Active: false, Future: false, Finalised: true), cancellationToken);
        if (competitions is null || competitions.Count == 0) return 0;

        var toProcess = new List<CompetitionDto>();

        foreach (var comp in competitions.Where(c => c.Id.HasValue))
        {
            var date = comp.Date;
            if (!date.HasValue)
            {
                var settingsQuery = new GetCompetitionSettingsByCompetitionIdQuery { CompetitionId = comp.Id!.Value.ToString() };
                var compSettings = await _mediator.Send(settingsQuery, cancellationToken);
                date = compSettings?.Date;
                comp.Date = date;
            }
            if (!date.HasValue) continue;

            var exists = await _payoutStore.ExistsAsync(comp.Id!.Value, date.Value, cancellationToken);
            if (!exists) toProcess.Add(comp);
        }

        if (toProcess.Count == 0) return 0;

        var processed = 0;
        var semaphore = new SemaphoreSlim(5, 5);
        var tasks = new List<Task>();

        var notEligible = toProcess.Where(tp => !eligibilityExpression.IsMatch(tp.Name)).ToList();  
        _logger.LogWarning("The following competitions are not eligible for prize payout processing based on the eligibility expression: {Expression}. Competitions: {Competitions}",
            eligibilityExpression.ToString(), string.Join(", ", notEligible.Select(ne => $"{ne.Name} (ID: {ne.Id})"))); 

        // Filter to include only eligible competitions
        toProcess = toProcess.Where(tp => eligibilityExpression.IsMatch(tp.Name)).ToList();
        _logger.LogInformation("Processing {Count} competitions for prize payouts.", toProcess.Count);  

        foreach (var comp in toProcess)
        {
            await semaphore.WaitAsync(cancellationToken);
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var ok = await ProcessOneAsync(comp, cancellationToken);
                    if (ok) Interlocked.Increment(ref processed);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process competition {CompetitionId}.", comp.Id);
                }
                finally
                {
                    semaphore.Release();
                }
            }, cancellationToken));
        }

        await Task.WhenAll(tasks);
        return processed;
    }

    private async Task<bool> ProcessOneAsync(CompetitionDto comp, CancellationToken cancellationToken)
    {
        var compId = comp.Id!.Value.ToString();
        var settings = await _mediator.Send(new GetCompetitionSettingsByCompetitionIdQuery { CompetitionId = compId }, cancellationToken);
        if (settings is null) return false;

        var leaderboard = await _mediator.Send(new GetCompetitionLeaderboardByCompetitionQuery { CompetitionId = compId }, cancellationToken);
        if (leaderboard is null) return false;

        var entrants = leaderboard.Players?.Where(p => p.PlayerId.HasValue).Select(p => p.PlayerId!.Value).Distinct().Count() ?? 0;

        var effective = _prizeConfig.GetEffective(comp.Date!.Value);
        var entryFee = ResolveEntryFee(settings, effective);
        var divisions = Math.Max(1, settings.Divisions ?? 1);
        var revenue = entrants * entryFee;
        var charityAmount = _prizeConfig.ComputeCharityAmount(effective, divisions, entrants, entryFee, revenue);

        var (ruleName, split) = _prizeConfig.GetSplitForFormat(effective, settings.Format);

        // Normalise and ensure the whole division pot is allocated.
        // If splits sum < 1, the remainder is added to the last place;
        // if > 1, they’re scaled to sum to 1.
        var normalisedSplits = NormaliseAndFill(split);
        var rule = new PrizeRule(ruleName, normalisedSplits, residualToLast: false);
        var maxPlaces = normalisedSplits.Count;

        var winnersPerDivision = BuildDivisionInputs(leaderboard);

        // Pay only the top N places, where N = split count.
        foreach (var d in winnersPerDivision)
        {
            d.OrderedByPosition = d.OrderedByPosition.Take(maxPlaces).ToList();
        }

        var input = new CompetitionPayoutInput
        {
            CompetitionId = comp.Id!.Value,
            CompetitionName = comp.Name,
            CompetitionDate = comp.Date!.Value.Date,
            Entrants = entrants,
            EntryFee = entryFee,
            Divisions = divisions,
            PayoutPercent = effective.PayoutPercent,
            CharityAmount = charityAmount,
            WinnersPerDivision = winnersPerDivision,
            Currency = "GBP",
            RuleSet = rule.Name
        };

        var result = _payoutCalculator.Compute(input, rule, maxPlaces);

        await _payoutStore.PersistAsync(result, cancellationToken);

        //await _payoutQueue.EnqueueAsync(new CompetitionPayoutCalculatedMessage
        //{
        //    CompetitionId = result.CompetitionId,
        //    CompetitionName = result.CompetitionName,
        //    CompetitionDate = result.CompetitionDate,
        //    CalculatedAtUtc = result.CalculatedAtUtc,
        //    Winners = result.Winners.Select(w => new CompetitionPayoutCalculatedMessage.Winner
        //    {
        //        DivisionNumber = w.DivisionNumber,
        //        Position = w.Position,
        //        CompetitorId = w.CompetitorId,
        //        CompetitorName = w.CompetitorName,
        //        Amount = w.Amount
        //    }).ToList()
        //}, cancellationToken);

        return true;
    }

    private static List<DivisionWinnersInput> BuildDivisionInputs(LeaderBoardDto leaderboard)
    {
        var list = new List<DivisionWinnersInput>();

        if (leaderboard.Divisions != null && leaderboard.Divisions.Count > 0)
        {
            foreach (var div in leaderboard.Divisions.OrderBy(d => d.Number))
            {
                var ordered = div.Players?
                    .Where(p => p.Position.HasValue)
                    .OrderBy(p => p.Position!.Value)
                    .Select(p => (CompetitorId: (p.PlayerId?.ToString() ?? $"N{p.Position}"), CompetitorName: p.PlayerName))
                    .ToList() ?? new List<(string, string)>();

                list.Add(new DivisionWinnersInput
                {
                    DivisionNumber = div.Number,
                    DivisionName = div.Limit.HasValue ? $"Division {div.Number} (≤ {div.Limit})" : $"Division {div.Number}",
                    OrderedByPosition = ordered
                });
            }
        }
        else
        {
            var ordered = leaderboard.Players?
                .Where(p => p.Position.HasValue)
                .OrderBy(p => p.Position!.Value)
                .Select(p => (CompetitorId: (p.PlayerId?.ToString() ?? $"N{p.Position}"), CompetitorName: p.PlayerName))
                .ToList() ?? new List<(string, string)>();

            list.Add(new DivisionWinnersInput
            {
                DivisionNumber = 1,
                DivisionName = "Division 1",
                OrderedByPosition = ordered
            });
        }

        return list;
    }

    private static IReadOnlyList<decimal> NormaliseAndFill(IReadOnlyList<decimal> splits)
    {
        var list = (splits ?? Array.Empty<decimal>()).ToList();
        if (list.Count == 0) return new List<decimal> { 1m };

        // Guard against negatives.
        for (int i = 0; i < list.Count; i++)
            if (list[i] < 0m) list[i] = 0m;

        var sum = list.Sum();
        if (sum <= 0m)
        {
            // All zeros – pay winner takes all.
            list[0] = 1m;
            for (int i = 1; i < list.Count; i++) list[i] = 0m;
            return list;
        }

        if (sum < 1m)
        {
            // Give the residual to the last paid place.
            list[list.Count - 1] += (1m - sum);
        }
        else if (sum > 1m)
        {
            // Scale to sum to 1.
            for (int i = 0; i < list.Count; i++)
                list[i] = list[i] / sum;
        }

        return list;
    }

    private static decimal ResolveEntryFee(CompetitionSettingsDto settings, PrizeRuleSet effective)
    {
        if (settings.SignupSettings?.Charges is { Count: > 0 })
        {
            var required = settings.SignupSettings.Charges.Where(c => c.Required == true && c.Amount.HasValue).Select(c => c.Amount!.Value).ToList();
            if (required.Count > 0) return required.Min();
        }
        return effective.EntryFeeFallback;
    }
}
