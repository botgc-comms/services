namespace BOTGC.API.Services.Queries;

public sealed record DetectorTriggerCommand(
    string DetectorName,
    int MemberNumber,
    string? CorrelationId,
    DateTimeOffset RequestedAtUtc);