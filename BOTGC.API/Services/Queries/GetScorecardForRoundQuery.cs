using BOTGC.API.Dto;
using MediatR;

namespace BOTGC.API.Services.Queries
{
    public record GetScorecardForRoundQuery: QueryBase<ScorecardDto?>
    {
        public required string RoundId { get; init; }
    }
}
