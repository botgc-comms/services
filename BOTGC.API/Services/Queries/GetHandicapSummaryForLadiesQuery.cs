using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries;

public record GetHandicapSummaryForLadiesQuery : QueryBase<List<PlayerHandicapSummaryDto>>;
