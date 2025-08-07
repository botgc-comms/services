using BOTGC.API.Common;

namespace BOTGC.API.Services.Queries
{
    public record SetMemberPropertiesQuery : QueryBase<bool>
    {
        public required MemberProperties Property { get; init; }
        public required int MemberId { get; init; }
        public required string Value { get; init; }
    }
}
