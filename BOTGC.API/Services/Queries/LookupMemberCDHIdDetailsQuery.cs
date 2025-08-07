using BOTGC.API.Dto;
using MediatR;

namespace BOTGC.API.Services.Queries
{
    public record GetTeeSheetByDateQuery: QueryBase<TeeSheetDto?>
    {
        public required DateTime Date { get; init; }
    }
}
