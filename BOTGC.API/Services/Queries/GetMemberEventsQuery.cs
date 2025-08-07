using BOTGC.API.Dto;
using MediatR;

namespace BOTGC.API.Services.Queries
{
    public record GetMemberEventsQuery: QueryBase<List<MemberEventDto>>
    {
        public required DateTime FromDate { get; init; }
        public required DateTime ToDate { get; init; }
    }
}
