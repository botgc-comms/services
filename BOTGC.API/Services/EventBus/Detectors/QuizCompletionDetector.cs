using System.Globalization;
using BOTGC.API.Common;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.EventBus;
using BOTGC.API.Services.EventBus.Events;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.Events.Detectors;

public sealed class QuizDetectorState
{
    public DateTimeOffset LastProcessedFinishedAtUtc { get; set; } = DateTimeOffset.MinValue;
}

[DetectorSchedule("0 22 * * *", runOnStartup: true)]
public sealed class QuizCompletionDetector(
    IDetectorStateStore stateStore,
    IEventPublisher publisher,
    IMediator mediator,
    IDistributedLockManager lockManager,
    IOptions<AppSettings> appSettings,
    ILogger<QuizCompletionDetector> logger)
    : MemberDetectorBase<QuizDetectorState>(stateStore, publisher, mediator, lockManager, appSettings, logger)
{
    public override string Name => "quiz-completion-detector";

    protected override async Task DetectAsync(
        MemberScope scopeKey,
        DetectorState<QuizDetectorState> state,
        IDetectorEventSink events,
        CancellationToken cancellationToken)
    {
        var memberId = scopeKey.MemberNumber;

        var detectorState = state.Value ??= new QuizDetectorState();

        var completedAttempts = await Mediator.Send(
            new GetCompletedQuizAttemptsQuery(memberId, detectorState.LastProcessedFinishedAtUtc),
            cancellationToken);

        if (completedAttempts.Count == 0)
        {
            return;
        }

        var maxFinishedAt = detectorState.LastProcessedFinishedAtUtc;

        foreach (var attempt in completedAttempts)
        {
            var passed = attempt.CorrectCount >= attempt.PassMark;

            QuizDifficulty quizDifficulty;
            Enum.TryParse<QuizDifficulty>(attempt.Difficulty, out quizDifficulty);

            events.Add(new QuizCompletedEvent
            {
                MemberId = memberId,
                AttemptId = attempt.AttemptId,
                FinishedAtUtc = attempt.FinishedAtUtc,
                TotalQuestions = attempt.TotalQuestions,
                CorrectCount = attempt.CorrectCount,
                PassMark = attempt.PassMark,
                Difficulty = quizDifficulty,
                Passed = passed
            });

            if (passed)
            {
                events.Add(new QuizPassedEvent
                {
                    MemberId = memberId,
                    AttemptId = attempt.AttemptId,
                    FinishedAtUtc = attempt.FinishedAtUtc,
                    TotalQuestions = attempt.TotalQuestions,
                    CorrectCount = attempt.CorrectCount,
                    Difficulty = quizDifficulty,
                    PassMark = attempt.PassMark
                });
            }

            if (attempt.FinishedAtUtc > maxFinishedAt)
            {
                maxFinishedAt = attempt.FinishedAtUtc;
            }
        }

        detectorState.LastProcessedFinishedAtUtc = maxFinishedAt;
    }
}
