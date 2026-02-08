using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BOTGC.API.Common;
using BOTGC.API.Models;
using BOTGC.API.Services.EventBus;

namespace BOTGC.API.Services;

public abstract class JuniorProgressPillarBase
{
    private readonly ProgressPillarAttribute _attribute;

    protected JuniorProgressPillarBase()
    {
        _attribute = GetType().GetCustomAttribute<ProgressPillarAttribute>(inherit: false)
            ?? throw new InvalidOperationException(
                $"Pillar '{GetType().Name}' must be decorated with {nameof(ProgressPillarAttribute)}.");
    }

    protected string PillarKey => _attribute.Key;
    protected string MilestoneEventType => _attribute.DefaultMilestoneEventType?.Trim() ?? string.Empty;

    public async Task<JuniorPillarProgressResult> EvaluateAsync(
        int memberId,
        string category,
        IReadOnlyList<EventStreamEntity> categoryEventsNewestFirst,
        JuniorPillarRule pillarRule,
        CancellationToken cancellationToken)
    {
        if (pillarRule is null)
        {
            throw new ArgumentNullException(nameof(pillarRule));
        }

        var weight = ClampPercent(pillarRule.WeightPercent);

        if (string.IsNullOrWhiteSpace(MilestoneEventType))
        {
            throw new InvalidOperationException(
                $"Pillar '{PillarKey}' has no milestone event type defined.");
        }

        var milestoneAchieved =
            categoryEventsNewestFirst.Any(e =>
                EventTypeMatcher.IsMatch(e.EventClrType, MilestoneEventType));

        if (milestoneAchieved)
        {
            return new JuniorPillarProgressResult(
                PillarKey: PillarKey,
                WeightPercent: weight,
                MilestoneAchieved: true,
                PillarCompletionFraction: 1.0d,
                ContributionPercent: weight);
        }

        var partial = Clamp01(
            await EvaluatePartialCompletionFractionAsync(
                memberId,
                category,
                categoryEventsNewestFirst,
                pillarRule,
                cancellationToken));

        var contribution = weight * partial;

        return new JuniorPillarProgressResult(
            PillarKey: PillarKey,
            WeightPercent: weight,
            MilestoneAchieved: false,
            PillarCompletionFraction: partial,
            ContributionPercent: contribution);
    }

    protected abstract Task<double> EvaluatePartialCompletionFractionAsync(
        int memberId,
        string category,
        IReadOnlyList<EventStreamEntity> categoryEventsNewestFirst,
        JuniorPillarRule pillarRule,
        CancellationToken cancellationToken);

    protected static string GetRequiredSetting(JuniorPillarRule rule, string key)
    {
        if (rule.Settings.TryGetValue(key, out var value) &&
            !string.IsNullOrWhiteSpace(value))
        {
            return value.Trim();
        }

        throw new InvalidOperationException(
            $"Missing required pillar setting '{key}' for pillar '{rule.Key}'.");
    }

    private static double Clamp01(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0.0d;
        }

        if (value < 0.0d)
        {
            return 0.0d;
        }

        if (value > 1.0d)
        {
            return 1.0d;
        }

        return value;
    }

    private static double ClampPercent(double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            return 0.0d;
        }

        if (value < 0.0d)
        {
            return 0.0d;
        }

        if (value > 100.0d)
        {
            return 100.0d;
        }

        return value;
    }
}
