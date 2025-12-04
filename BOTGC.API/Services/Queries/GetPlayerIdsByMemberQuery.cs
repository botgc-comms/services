using BOTGC.API.Dto;
using MediatR;

namespace BOTGC.API.Services.Queries;

public record GetPlayerIdsByMemberQuery : QueryBase<List<PlayerIdLookupDto>>;
