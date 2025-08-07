using BOTGC.API.Dto;
using MediatR;

namespace BOTGC.API.Services.Queries
{
    public record GetMembershipReportQuery : QueryBase<List<MemberDto>>;
}
