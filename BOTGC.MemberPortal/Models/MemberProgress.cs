using System.Text.Json.Serialization;

namespace BOTGC.MemberPortal.Models;

public sealed class MemberProgress
{
    public required Guid EventId { get; init; }
    public required int MemberId { get; init; }
    public required DateTimeOffset OccurredAtUtc { get; init; }

    public required string Category { get; init; }
    public required double PercentComplete { get; init; }

    public required IReadOnlyList<MemberProgressPillar> Pillars { get; init; }

    public string? PreviousSignature { get; init; }
    public string? NewSignature { get; init; }

    public required int ProgressPercentRounded { get; init; }
}

public sealed class MemberProgressPillar
{
    public required string PillarKey { get; init; }
    public required double WeightPercent { get; init; }
    public required bool MilestoneAchieved { get; init; }
    public required double PillarCompletionFraction { get; init; }
    public required double ContributionPercent { get; init; }
}

public sealed class JuniorProgressChangedEventDto
{
    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; }

    [JsonPropertyName("memberId")]
    public int MemberId { get; set; }

    [JsonPropertyName("occurredAtUtc")]
    public DateTimeOffset OccurredAtUtc { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

    [JsonPropertyName("percentComplete")]
    public double PercentComplete { get; set; }

    [JsonPropertyName("pillars")]
    public List<JuniorProgressPillarDto> Pillars { get; set; } = new();

    [JsonPropertyName("previousSignature")]
    public string? PreviousSignature { get; set; }

    [JsonPropertyName("newSignature")]
    public string? NewSignature { get; set; }
}

public sealed class JuniorProgressPillarDto
{
    [JsonPropertyName("pillarKey")]
    public string PillarKey { get; set; } = string.Empty;

    [JsonPropertyName("weightPercent")]
    public double WeightPercent { get; set; }

    [JsonPropertyName("milestoneAchieved")]
    public bool MilestoneAchieved { get; set; }

    [JsonPropertyName("pillarCompletionFraction")]
    public double PillarCompletionFraction { get; set; }

    [JsonPropertyName("contributionPercent")]
    public double ContributionPercent { get; set; }
}
