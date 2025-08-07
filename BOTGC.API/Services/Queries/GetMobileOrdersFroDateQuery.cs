using BOTGC.API.Dto;
using MediatR;

namespace BOTGC.API.Services.Queries
{
    public record GetMobileOrdersForDateQuery: QueryBase<List<SecurityLogEntryDto>>
    {
        public required DateTime? ForDate { get; init; }
    }
}
