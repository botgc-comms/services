using BOTGC.API.Models;

namespace BOTGC.API.Interfaces;

public interface IQuizAttemptReadStore
{
    Task<IReadOnlyList<QuizAttemptEntity>> ListCompletedAttemptsAsync(
        string userId,
        DateTimeOffset? sinceUtc,
        int take,
        CancellationToken cancellationToken);
}
