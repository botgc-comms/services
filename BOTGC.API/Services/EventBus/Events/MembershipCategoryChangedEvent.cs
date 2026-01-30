using BOTGC.API.Interfaces;

namespace BOTGC.API.Services.EventBus.Events;

[EventType("membership.category-changed", 1)]
public sealed class MembershipCategoryChangedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public int MemberId { get; init; }

    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public required string FromCategory { get; init; }

    public required string ToCategory { get; init; }

    public string? Reason { get; init; }

    public string? TriggeredBy { get; init; }
}
