using BOTGC.API.Dto;
using MediatR;

namespace BOTGC.API.Services.Queries
{
    public record GetCurrentMembersQuery : QueryBase<List<MemberDto>>;
}
