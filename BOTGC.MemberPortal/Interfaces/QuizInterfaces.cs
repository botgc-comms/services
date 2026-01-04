using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Interfaces;

public interface IQuizContentSource
{
    Task<QuizContentSnapshot> LoadAsync(CancellationToken ct = default);
}

public interface IQuizQuestionProvider
{
    Task<QuizContentSnapshot?> GetSnapshotAsync(CancellationToken ct = default);
}

public interface IQuizAttemptRepository
{
    Task<QuizAttempt?> GetInProgressAttemptAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlyList<QuizAttemptSummary>> ListCompletedAttemptsAsync(string userId, int take, CancellationToken ct = default);
    Task<QuizAttemptDetails?> GetAttemptDetailsAsync(string userId, string attemptId, CancellationToken ct = default);

    Task<QuizAttempt> CreateAttemptAsync(QuizAttempt attempt, CancellationToken ct = default);
    Task<QuestionAttempt?> GetAnswerAsync(string attemptId, string questionId, CancellationToken ct = default);
    Task UpsertAnswerAsync(string attemptId, int questionIndex, QuestionAttempt answer, CancellationToken ct = default);
    Task UpdateAttemptAsync(QuizAttempt attempt, CancellationToken ct = default);

    Task<IReadOnlySet<string>> GetPreviouslyAskedQuestionIdsAsync(string userId, CancellationToken ct = default);
    Task<IReadOnlySet<string>> GetPreviouslyCorrectQuestionIdsAsync(string userId, CancellationToken ct = default);
}
