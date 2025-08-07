using BOTGC.API.Dto;
using MediatR;

namespace BOTGC.API.Services.Queries
{
    public record GetRoundsByMemberIdQuery: QueryBase<List<RoundDto>>
    {
        public required string MemberId { get; init; }
    }
}
