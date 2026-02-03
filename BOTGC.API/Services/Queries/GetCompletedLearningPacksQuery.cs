using BOTGC.API.Models;

namespace BOTGC.API.Services.Queries;

public sealed record GetCompletedLearningPacksQuery(
    int MemberId,
    DateTimeOffset? SinceUtc = null,
    int Take = 50)
    : QueryBase<IReadOnlyList<CompletedLearningPackDto>>;

