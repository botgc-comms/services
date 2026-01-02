namespace BOTGC.MemberPortal.Models;

public enum QuizDifficulty
{
    Easy = 1,
    Medium = 2,
    Hard = 3,
}

public sealed record QuizChoice(
    string Id,
    string Text
);

public sealed record QuizQuestion(
    string Id,
    QuizDifficulty Difficulty,
    string Question,
    string Type,
    IReadOnlyList<QuizChoice> Choices,
    string CorrectAnswer,
    string Explanation,
    string ImageUrl,
    string ImageAlt
);

public sealed record QuizContentSnapshot(
    DateTimeOffset LoadedAtUtc,
    IReadOnlyList<QuizQuestion> Questions
);

public sealed record StartQuizRequest(
    string UserId,
    int QuestionCount,
    int PassMark,
    IReadOnlyCollection<QuizDifficulty>? AllowedDifficulties
);

public sealed record StartQuizResult(
    bool StartedNew,
    QuizAttempt Attempt
);

public sealed record QuizAttempt(
    string AttemptId,
    string UserId,
    string Status,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    int TotalQuestions,
    int CorrectCount,
    int PassMark,
    string ContentVersion,
    IReadOnlyList<string> QuestionIdsInOrder
);

public sealed record QuizAttemptSummary(
    string AttemptId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset FinishedAtUtc,
    int TotalQuestions,
    int CorrectCount,
    int PassMark
);

public sealed record QuestionAttempt(
    string QuestionId,
    string SelectedAnswerId,
    bool IsCorrect,
    DateTimeOffset AnsweredAtUtc
);

public sealed record QuizAttemptDetails(
    QuizAttempt Attempt,
    IReadOnlyList<QuestionAttempt> Answers
);

public sealed record AnswerQuestionResult(
    string AttemptId,
    string QuestionId,
    string SelectedAnswerId,
    bool IsCorrect,
    string CorrectAnswerId,
    string Explanation,
    int CorrectCount,
    int TotalQuestions,
    int PassMark,
    bool IsFinished
);

public sealed class JuniorQuizOptions
{
    public string CacheKeyPrefix { get; init; } = "junior-quiz";
    public TimeSpan CacheTtl { get; init; } = TimeSpan.FromHours(12);
}

public sealed class GitHubQuizSourceOptions
{
    public required string Owner { get; init; }
    public required string Repo { get; init; }
    public string Ref { get; init; } = "main";
    public string RootPath { get; init; } = "questions";
    public string? Token { get; init; }
}

public sealed class FileSystemQuizSourceOptions
{
    public required string RootPath { get; init; }
    public string QuestionsFolderName { get; init; } = "questions";
    public string ImageFileName { get; init; } = "image.png";
}

public sealed class TableStorageQuizOptions
{
    public required string ConnectionString { get; init; }
    public string AttemptsTableName { get; init; } = "QuizAttempts";
    public string AnswersTableName { get; init; } = "QuizAnswers";
}
