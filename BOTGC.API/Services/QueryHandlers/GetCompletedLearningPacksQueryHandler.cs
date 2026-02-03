using System.Globalization;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class GetCompletedLearningPacksQueryHandler(
    ILearningPackProgressReadStore store)
    : QueryHandlerBase<GetCompletedLearningPacksQuery, IReadOnlyList<CompletedLearningPackDto>>
{
    private readonly ILearningPackProgressReadStore _store = store ?? throw new ArgumentNullException(nameof(store));

    public override async Task<IReadOnlyList<CompletedLearningPackDto>> Handle(
        GetCompletedLearningPacksQuery request,
        CancellationToken cancellationToken)
    {
        var take = request.Take <= 0 ? 50 : Math.Min(request.Take, 500);

        var rows = await _store.ListCompletedPacksAsync(
            request.MemberId.ToString(CultureInfo.InvariantCulture),
            request.SinceUtc,
            take,
            cancellationToken);

        return rows
            .Select(x => new CompletedLearningPackDto
            {
                PackId = x.PackId,
                CompletedAtUtc = x.CompletedAtUtc
            })
            .OrderBy(x => x.CompletedAtUtc)
            .ToArray();
    }
}