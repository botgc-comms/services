using BOTGC.API.Dto;
using MediatR;

namespace BOTGC.API.Services.Queries
{
    public record GetSubscriptionPaymentsByDateRangeQuery : QueryBase<List<SubscriptionPaymentDto>>
    {
        public required DateTime FromDate { get; init; }
        public required DateTime ToDate { get; init; }
    }
}
