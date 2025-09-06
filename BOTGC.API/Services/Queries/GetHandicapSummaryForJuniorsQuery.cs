using BOTGC.API.Dto;
using MediatR;

namespace BOTGC.API.Services.Queries
{
    public record GetHandicapSummaryForJuniorsQuery : QueryBase<List<PlayerHandicapSummaryDto>>;
}
