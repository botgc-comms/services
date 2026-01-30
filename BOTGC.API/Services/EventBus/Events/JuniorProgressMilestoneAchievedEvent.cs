using BOTGC.API.Interfaces;

namespace BOTGC.API.Services.EventBus.Events;

[EventType("junior.progress-milestone-achieved", 1)]
public sealed class JuniorProgressMilestoneAchievedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public int MemberId { get; init; }

    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public required string FromCategory { get; init; }

    public required string ToCategory { get; init; }

    public required string MilestoneName { get; init; }

    public string? CriteriaSummary { get; init; }

    public DateTimeOffset WindowStartUtc { get; init; }

    public DateTimeOffset WindowEndUtc { get; init; }
}
