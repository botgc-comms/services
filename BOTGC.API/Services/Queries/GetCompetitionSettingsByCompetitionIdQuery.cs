using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries;

public record GetCompetitionSettingsByCompetitionIdQuery : QueryBase<CompetitionSettingsDto?>
{
    public required string CompetitionId { get; init; }
}
