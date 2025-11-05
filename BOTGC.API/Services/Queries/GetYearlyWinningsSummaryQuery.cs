using BOTGC.API.Dto;
using MediatR;

namespace BOTGC.API.Services.Queries
{
    public sealed record GetYearlyWinningsSummaryQuery(int Year) : QueryBase<YearlyWinningsSummaryDto>;
}
