using System.Reflection;
using BOTGC.API.Common;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.EventBus;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services;

public sealed class JuniorCategoryProgressCalculator : IJuniorCategoryProgressCalculator
{
    private readonly IMemberEventWindowReader _windowReader;
    private readonly AppSettings _app;
    private readonly IMediator _mediator;   
    private readonly IReadOnlyDictionary<string, JuniorProgressPillarBase> _pillars;

    public JuniorCategoryProgressCalculator(
        IMemberEventWindowReader windowReader,
        IMediator mediator,
        IOptions<AppSettings> appSettings,
        IEnumerable<JuniorProgressPillarBase> pillars)
    {
        _windowReader = windowReader ?? throw new ArgumentNullException(nameof(windowReader));
        _app = appSettings?.Value ?? throw new ArgumentNullException(nameof(appSettings));
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

        _pillars = (pillars ?? throw new ArgumentNullException(nameof(pillars)))
            .Select(p =>
            {
                var attr = p.GetType().GetCustomAttribute<ProgressPillarAttribute>(inherit: false)
                    ?? throw new InvalidOperationException($"Pillar '{p.GetType().FullName}' is missing {nameof(ProgressPillarAttribute)}.");

                if (string.IsNullOrWhiteSpace(attr.Key))
                {
                    throw new InvalidOperationException($"Pillar '{p.GetType().FullName}' has an empty '{nameof(ProgressPillarAttribute)}.{nameof(ProgressPillarAttribute.Key)}'.");
                }

                return (Key: attr.Key.Trim(), Pillar: p);
            })
            .GroupBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                g => g.Key,
                g =>
                {
                    var list = g.Select(x => x.Pillar).ToList();

                    if (list.Count != 1)
                    {
                        throw new InvalidOperationException(
                            $"Multiple pillar evaluators registered for key '{g.Key}': {string.Join(", ", list.Select(x => x.GetType().FullName))}");
                    }

                    return list[0];
                },
                StringComparer.OrdinalIgnoreCase);
    }

    public async Task<JuniorCategoryProgressResult> CalculateAsync(int memberId, CancellationToken cancellationToken)
    {
        var take = _app.Events?.JuniorProgress?.CategoryWindowTake ?? 500;

        var window = await _windowReader.GetLatestCategoryWindowAsync(memberId, take, cancellationToken);

        var category = window.Category;
        if (string.IsNullOrWhiteSpace(category))
        {
            var member = await _mediator.Send(new GetMemberQuery
            {
                MemberNumber = memberId,
                Cache = new CacheOptions(NoCache: true, WriteThrough: true)
            }, cancellationToken);

            category = member?.MembershipCategory;
        }

        if (string.IsNullOrWhiteSpace(category))
        {
            return new JuniorCategoryProgressResult(
                MemberId: memberId,
                Category: string.Empty,
                PercentComplete: 0.0d,
                Pillars: Array.Empty<JuniorPillarProgressResult>());
        }

        var rule = _app.Events?.JuniorProgress?.Categories
            .FirstOrDefault(c => string.Equals(c.MembershipCategory, category, StringComparison.OrdinalIgnoreCase));

        if (rule is null || rule.Pillars.Count == 0)
        {
            return new JuniorCategoryProgressResult(
                MemberId: memberId,
                Category: category,
                PercentComplete: 0.0d,
                Pillars: Array.Empty<JuniorPillarProgressResult>());
        }

        var results = new List<JuniorPillarProgressResult>(rule.Pillars.Count);

        foreach (var pillarRule in rule.Pillars)
        {
            var key = pillarRule.Key?.Trim();

            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (!_pillars.TryGetValue(key, out var evaluator))
            {
                throw new InvalidOperationException($"No pillar evaluator registered for key '{key}'.");
            }

            results.Add(await evaluator.EvaluateAsync(
                memberId: memberId,
                category: category,
                categoryEventsNewestFirst: window.EventsNewestFirst,
                pillarRule: pillarRule,
                cancellationToken: cancellationToken));
        }

        var percent = results.Sum(r => r.ContributionPercent);

        if (double.IsNaN(percent) || double.IsInfinity(percent) || percent < 0.0d)
        {
            percent = 0.0d;
        }

        if (percent > 100.0d)
        {
            percent = 100.0d;
        }

        return new JuniorCategoryProgressResult(
            MemberId: memberId,
            Category: category,
            PercentComplete: percent,
            Pillars: results);
    }
}
