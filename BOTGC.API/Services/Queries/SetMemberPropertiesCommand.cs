using BOTGC.API.Dto;
using BOTGC.API.IGScrapers;

namespace BOTGC.API.Services.Queries
{
    public record SetMemberPropertiesCommand : QueryBase<bool>
    {
        public required MemberProperties Property { get; init; }
        public required int MemberId { get; init; }
        public required string Value { get; init; }
    }
}
