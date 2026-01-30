using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries;

public sealed record GetCompletedQuizAttemptsQuery(
    int MemberId,
    DateTimeOffset? SinceUtc = null,
    int Take = 50
) : QueryBase<IReadOnlyList<CompletedQuizAttemptDto>>;