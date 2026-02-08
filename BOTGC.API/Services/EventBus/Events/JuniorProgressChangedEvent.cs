using BOTGC.API.Interfaces;
using BOTGC.API.Models;

namespace BOTGC.API.Services.EventBus.Events;

[EventType("junior-progress.changed", 1)]
public sealed class JuniorProgressChangedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public int MemberId { get; init; }

    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public required string Category { get; init; }

    public double PercentComplete { get; init; }

    public required IReadOnlyList<JuniorPillarProgressResult> Pillars { get; init; }

    public string? PreviousSignature { get; init; }

    public required string NewSignature { get; init; }
    public IReadOnlyList<string>? RemainingTasks { get; init; }
}