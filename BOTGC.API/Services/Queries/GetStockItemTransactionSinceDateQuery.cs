using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public sealed record GetStockItemTransactionSinceDateQuery : QueryBase<List<StockItemTransactionReportEntryDto>>
    {
        public DateTime FromDate { get; set; }
        public int ZRead { get; set; } = 4498;
        public int TillConfigId { get; set; } = 0;
        public int StockItemId { get; set; } = 0;
    };
}
