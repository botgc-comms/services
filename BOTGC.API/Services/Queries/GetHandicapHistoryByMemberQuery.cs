using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public record GetHandicapHistoryByMemberQuery : QueryBase<PlayerHandicapSummaryDto?>
    {
        public required int MemberId { get; init; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
