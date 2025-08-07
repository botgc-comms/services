using BOTGC.API.Dto;
using MediatR;

namespace BOTGC.API.Services.Queries
{
    public record GetCompetitionSettingsByCompetitionIdQuery : QueryBase<CompetitionSettingsDto?>
    {
        public required string CompetitionId { get; init; }
    }
}
