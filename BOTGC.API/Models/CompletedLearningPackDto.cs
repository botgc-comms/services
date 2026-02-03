namespace BOTGC.API.Models;

public sealed class CompletedLearningPackDto
{
    public required string PackId { get; init; }
    public DateTimeOffset CompletedAtUtc { get; init; }
}
