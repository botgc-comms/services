using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries;

public record GetParentsQuery(): QueryBase<List<ParentChildDto>>;