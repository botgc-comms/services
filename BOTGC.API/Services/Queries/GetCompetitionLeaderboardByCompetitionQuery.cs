using BOTGC.API.Dto;
using MediatR;

namespace BOTGC.API.Services.Queries
{
    public record GetCompetitionLeaderboardByCompetitionQuery: QueryBase<LeaderBoardDto?>
    {
        public required String CompetitionId { get; init; }
    }
}
