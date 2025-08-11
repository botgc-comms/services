using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public record GetWaitingMembersQuery : QueryBase<List<MemberDto>>;
}
