using BOTGC.API.Dto;
using MediatR;

namespace BOTGC.API.Services.Queries
{
    public record GetNewMembersQuery : QueryBase<List<MemberDetailsDto>>;
}
