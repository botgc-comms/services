using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries;

public record GetMemberQuery : QueryBase<MemberDetailsDto?>
{
    public int? MemberNumber { get; set; }
    public int? PlayerId { get; set; }
}
