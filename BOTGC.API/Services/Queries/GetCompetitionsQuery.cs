using BOTGC.API.Dto;
using MediatR;

namespace BOTGC.API.Services.Queries
{
    public record GetCompetitionsQuery(bool Active = true, bool Future = true, bool Finalised = false) : QueryBase<List<CompetitionDto>>;
}
