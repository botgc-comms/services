using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using System.Threading;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class ProcessFinalisedCompetitionPayoutsHandler(
        IOptions<AppSettings> settings,
        ILogger<ProcessFinalisedCompetitionPayoutsHandler> logger,
        IMediator mediator,
        ICompetitionPayoutCalculator payoutCalculator,
        ICompetitionPayoutStore payoutStore,
        IPrizeConfigProvider prizeConfig,
        IQueueService<SendPrizeNotificationEmailCommand> playerNotificationsQueueService,
        IQueueService<ProcessPrizeInvoiceCommand> prizeInvoiceQueueService,
        IQueueService<ProcessCompetitionWinningsBatchCompletedCommand> prizeCalculationsCompleteQueueService
    ) : QueryHandlerBase<ProcessFinalisedCompetitionPayoutsCommand, int>
{
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<ProcessFinalisedCompetitionPayoutsHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ICompetitionPayoutCalculator _payoutCalculator = payoutCalculator ?? throw new ArgumentNullException(nameof(payoutCalculator));
    private readonly ICompetitionPayoutStore _payoutStore = payoutStore ?? throw new ArgumentNullException(nameof(payoutStore));
    private readonly IPrizeConfigProvider _prizeConfig = prizeConfig ?? throw new ArgumentNullException(nameof(prizeConfig));
    private readonly IQueueService<SendPrizeNotificationEmailCommand> _playerNotificationsQueueService = playerNotificationsQueueService;
    private readonly IQueueService<ProcessPrizeInvoiceCommand> _prizeInvoiceQueueService = prizeInvoiceQueueService;
    private readonly IQueueService<ProcessCompetitionWinningsBatchCompletedCommand> _prizeCalculationsCompleteQueueService = prizeCalculationsCompleteQueueService;

    public override async Task<int> Handle(ProcessFinalisedCompetitionPayoutsCommand request, CancellationToken cancellationToken)
    {
        var eligibilityExpression = new Regex(_settings.PrizePayout.EligibilityExpression ?? ".*", RegexOptions.IgnoreCase);
        var excludeExpression = new Regex(_settings.PrizePayout.ExclusionExpression ?? "^$", RegexOptions.IgnoreCase);

        var competitions = await _mediator.Send(
            new GetCompetitionsQuery(Active: false, Future: false, Finalised: true),
            cancellationToken);

        if (competitions is null || competitions.Count == 0)
            return 0;

        var childIds = competitions
            .Where(c => c.MultiPartCompetition is { Count: > 0 })
            .SelectMany(c => c.MultiPartCompetition!.Keys)
            .Select(k => int.TryParse(k, out var id) ? id : 0)
            .Where(id => id > 0)
            .ToHashSet();

        if (childIds.Count > 0)
        {
            _logger.LogInformation("Excluding {Count} child competitions from payout processing: {Ids}.",
                childIds.Count, string.Join(", ", childIds.OrderBy(x => x)));
        }

        competitions = competitions
            .Where(c => c.Id.HasValue && !childIds.Contains(c.Id.Value))
            .ToList();

        if (competitions.Count == 0)
            return 0;

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

            if (!date.HasValue)
                continue;

            var exists = await _payoutStore.ExistsAsync(comp.Id!.Value, date.Value, cancellationToken);
            if (!exists)
                toProcess.Add(comp);
        }

        if (toProcess.Count == 0)
            return 0;

        var notEligible = toProcess.Where(tp => !eligibilityExpression.IsMatch(tp.Name)).ToList();
        if (notEligible.Count > 0)
        {
            _logger.LogWarning(
                "The following competitions are not eligible for prize payout processing based on the expression '{Expression}': {Competitions}",
                eligibilityExpression,
                string.Join(", ", notEligible.Select(ne => $"{ne.Name} (ID: {ne.Id})")));
        }

        var excluded = toProcess.Where(tp => excludeExpression.IsMatch(tp.Name)).ToList();
        if (excluded.Count > 0)
        {
            _logger.LogWarning(
                "The following competitions have been excluded for prize payout processing based on the expression '{Expression}': {Competitions}",
                excludeExpression,
                string.Join(", ", excluded.Select(ne => $"{ne.Name} (ID: {ne.Id})")));
        }

        toProcess = toProcess.Where(tp => eligibilityExpression.IsMatch(tp.Name)).ToList();
        toProcess = toProcess.Where(tp => !excludeExpression.IsMatch(tp.Name)).ToList();

        if (toProcess.Count == 0)
            return 0;

        _logger.LogInformation("Processing {Count} competitions for prize payouts.", toProcess.Count);

        var semaphore = new SemaphoreSlim(5, 5);
        var tasks = new List<Task>();
        var processedCount = 0;
        var processedCompetitionIds = new ConcurrentBag<int>();

        foreach (var comp in toProcess)
        {
            await semaphore.WaitAsync(cancellationToken);

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var ok = await ProcessOneAsync(comp, cancellationToken);
                    if (ok)
                    {
                        Interlocked.Increment(ref processedCount);
                        processedCompetitionIds.Add(comp.Id!.Value);
                    }
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

        var completedIds = processedCompetitionIds
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        if (completedIds.Count > 0)
        {
            var cmd = new ProcessCompetitionWinningsBatchCompletedCommand(
                completedIds,
                DateTime.UtcNow);

            await _prizeCalculationsCompleteQueueService.EnqueueAsync(cmd, cancellationToken);

            _logger.LogInformation(
                "Enqueued ProcessCompetitionWinningsBatchCompletedCommand for competitions: {Ids}.",
                string.Join(", ", completedIds));
        }

        return processedCount;
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

        var normalisedSplits = NormaliseAndFill(split);
        var rule = new PrizeRule(ruleName, normalisedSplits, residualToLast: false);
        var maxPlaces = normalisedSplits.Count;

        var winnersPerDivision = BuildDivisionInputs(leaderboard);

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

        if (_settings.PrizePayout.SendPlayerEmails)
        {
            await _playerNotificationsQueueService.EnqueueManyAsync(
                result.Winners.Select(w =>
                    new SendPrizeNotificationEmailCommand(
                        w.CompetitorId,
                        w.Position,
                        default,
                        w.CompetitorName,
                        input.CompetitionDate,
                        input.CompetitionName,
                        w.Amount)),
                cancellationToken);
        }

        if (_settings.PrizePayout.GenerateInvoices)
        {
            await _prizeInvoiceQueueService.EnqueueAsync(
                new ProcessPrizeInvoiceCommand(comp.Id!.Value),
                cancellationToken);
        }

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

        for (int i = 0; i < list.Count; i++)
            if (list[i] < 0m) list[i] = 0m;

        var sum = list.Sum();
        if (sum <= 0m)
        {
            list[0] = 1m;
            for (int i = 1; i < list.Count; i++) list[i] = 0m;
            return list;
        }

        if (sum < 1m)
        {
            list[list.Count - 1] += (1m - sum);
        }
        else if (sum > 1m)
        {
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
