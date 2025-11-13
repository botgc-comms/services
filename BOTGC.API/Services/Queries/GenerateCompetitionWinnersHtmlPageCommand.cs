using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries;

public sealed record GenerateCompetitionWinnersHtmlPageCommand(int CompetitionId)
    : QueryBase<string?>;
