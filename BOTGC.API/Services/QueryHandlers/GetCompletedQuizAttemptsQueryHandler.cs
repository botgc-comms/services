using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class GetCompletedQuizAttemptsQueryHandler(
    IQuizAttemptReadStore store) : QueryHandlerBase<GetCompletedQuizAttemptsQuery, IReadOnlyList<CompletedQuizAttemptDto>>
{
    private readonly IQuizAttemptReadStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public override async Task<IReadOnlyList<CompletedQuizAttemptDto>> Handle(GetCompletedQuizAttemptsQuery request, CancellationToken cancellationToken)
    {
        var take = request.Take <= 0 ? 50 : Math.Min(request.Take, 500);

        var attempts = await _store.ListCompletedAttemptsAsync(request.MemberId.ToString(), request.SinceUtc, take, cancellationToken);

        return attempts
             .Where(x => x.FinishedAtUtc.HasValue)
             .Select(x => new CompletedQuizAttemptDto
             {
                 AttemptId = x.AttemptId,
                 StartedAtUtc = x.StartedAtUtc,
                 FinishedAtUtc = x.FinishedAtUtc!.Value,
                 TotalQuestions = x.TotalQuestions,
                 CorrectCount = x.CorrectCount,
                 PassMark = x.PassMark,
                 Difficulty = x.Difficulty ?? string.Empty,
             })
             .ToArray();
    }
}