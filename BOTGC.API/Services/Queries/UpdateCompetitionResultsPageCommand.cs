using MediatR;

namespace BOTGC.API.Services.Queries
{
    public sealed record UpdateCompetitionResultsPageCommand : QueryBase<bool>;
}
