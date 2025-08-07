using BOTGC.API.Dto;
using MediatR;

namespace BOTGC.API.Services.Queries
{
    public record LookupMemberCDHIdDetailsQuery: QueryBase<MemberCDHLookupDto?>
    {
        public required String CDHId { get; init; }
    }
}
