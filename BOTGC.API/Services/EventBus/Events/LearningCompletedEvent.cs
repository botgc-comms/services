using BOTGC.API.Interfaces;

namespace BOTGC.API.Services.EventBus.Events;

[EventType("learning.completed", 1)]
public sealed class LearningCompletedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public int MemberId { get; init; }

    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public required string CourseName { get; init; }

    public required string CompletedBy { get; init; }

    public DateTimeOffset CompletedAtUtc { get; init; }
}