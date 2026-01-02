using BOTGC.MemberPortal.Interfaces;
using BOTGC.MemberPortal.Models;

namespace BOTGC.MemberPortal.Services;

public sealed class CachedQuizQuestionProvider : IQuizQuestionProvider
{
    private readonly QuizContentCache _cache;

    public CachedQuizQuestionProvider(QuizContentCache cache)
    {
        _cache = cache;
    }

    public async Task<IReadOnlyList<QuizQuestion>> GetAllQuestionsAsync(CancellationToken ct = default)
    {
        var snapshot = await _cache.GetCachedAsync(ct);
        return snapshot?.Questions ?? Array.Empty<QuizQuestion>();
    }
}
