using BOTGC.API.Interfaces;

namespace BOTGC.API.Services.EventBus.Events;

// Raised when a parent completes a learning pack
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

// raised against a child when their parent completes a learning pack
[EventType("learning.parent.completed", 1)]
public sealed class ParentLearningCompletedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public int MemberId { get; init; }                  
    public int ParentMemberId { get; init; }            

    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public required string CourseName { get; init; }    
    public required string CompletedBy { get; init; }   
    public DateTimeOffset CompletedAtUtc { get; init; } 
}
