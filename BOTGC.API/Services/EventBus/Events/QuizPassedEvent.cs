using BOTGC.API.Common;
using BOTGC.API.Interfaces;

namespace BOTGC.API.Services.EventBus.Events;


[EventType("quiz.passed", 1)]
public sealed class QuizPassedEvent : IEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public int MemberId { get; init; }

    public DateTimeOffset OccurredAtUtc { get; init; } = DateTimeOffset.UtcNow;

    public required string AttemptId { get; init; }

    public required DateTimeOffset FinishedAtUtc { get; init; }

    public int TotalQuestions { get; init; }

    public int CorrectCount { get; init; }

    public int PassMark { get; init; }

    public QuizDifficulty Difficulty { get; init; }
}
