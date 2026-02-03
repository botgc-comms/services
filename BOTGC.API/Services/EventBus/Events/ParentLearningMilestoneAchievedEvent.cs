using BOTGC.API.Interfaces;

namespace BOTGC.API.Services.EventBus.Events;

[EventType("learning.milestone.parent.completed", 1)]
public sealed class ParentLearningMilestoneAchievedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public int MemberId { get; init; }

    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public int ParentMemberId { get; init; }

    public required string ChildCategory { get; init; }
}
