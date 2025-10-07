using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public sealed record GetStockTakesQuery : QueryBase<List<StockTakeSummaryDto>>
    {
        public DateTime? FromDate { get; set; } = null;
        public DateTime? ToDate { get; set; } = null;
        public int ZRead { get; set; } = 4498;
        public int TillConfigId { get; set; } = 0;
        public int StockItemId { get; set; } = 0;
    };
}
