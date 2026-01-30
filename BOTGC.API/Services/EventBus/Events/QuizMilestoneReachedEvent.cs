using BOTGC.API.Common;
using BOTGC.API.Interfaces;

namespace BOTGC.API.Services.EventBus.Events;

[EventType("quiz.milestone.reached", 1)]
public sealed class QuizMilestoneReachedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public int MemberId { get; init; }
    public string Category { get; init; } = string.Empty;

    public int RequiredPassedCount { get; init; }
    public QuizDifficulty MinimumDifficulty { get; init; }

    public int PassedCountInWindow { get; init; }

    public DateTimeOffset WindowStartUtc { get; init; }
    public DateTimeOffset WindowEndUtc { get; init; }

    public DateTimeOffset OccurredAtUtc { get; init; }
    public string MilestoneAchieved { get; set; }
}
