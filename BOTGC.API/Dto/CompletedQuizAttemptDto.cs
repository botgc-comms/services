namespace BOTGC.API.Dto;

public sealed class CompletedQuizAttemptDto : HateoasResource
{
    public string AttemptId { get; init; } = string.Empty;
    public DateTimeOffset StartedAtUtc { get; init; }
    public DateTimeOffset FinishedAtUtc { get; init; }
    public int TotalQuestions { get; init; }
    public int CorrectCount { get; init; }
    public int PassMark { get; init; }
    public string Difficulty { get; init; } = string.Empty;
}