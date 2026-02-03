namespace BOTGC.API.Models;

public sealed class LearningPackProgressRow
{
    public required string PackId { get; init; }
    public DateTimeOffset CompletedAtUtc { get; init; }
}
