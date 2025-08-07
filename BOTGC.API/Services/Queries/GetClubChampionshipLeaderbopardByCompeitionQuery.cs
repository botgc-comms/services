using BOTGC.API.Dto;
using MediatR;

namespace BOTGC.API.Services.Queries
{
    public record GetClubChampionshipLeaderboardByCompetitionQuery: QueryBase<ClubChampionshipLeaderBoardDto?>
    {
        public required String CompetitionId { get; init; }
    }
}
