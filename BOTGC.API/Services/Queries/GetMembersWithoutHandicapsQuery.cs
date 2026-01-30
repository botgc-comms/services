namespace BOTGC.API.Services.Queries;

public record GetMembersWithoutHandicapsQuery : QueryBase<List<MemberWithoutHandicapDto>>
{
    public int? MemberNumber { get; set; }
    public int? PlayerId { get; set; }
}
