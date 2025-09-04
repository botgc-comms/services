using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public record GetMemberQuery : QueryBase<MemberDetailsDto?>
    {
        public required int MemberNumber { get; init; }
    }
}
