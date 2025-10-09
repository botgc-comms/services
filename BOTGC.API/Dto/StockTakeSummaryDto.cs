namespace BOTGC.API.Dto
{
    public sealed class StockTakeSummaryDto: HateoasResource
    {
        public int StockItemId { get; set; }
        public string? Name { get; set; }   
        public string? Unit { get; set; }
        public string? Division { get; set; }
        public decimal? CurrentQuantity { get; set; }
        public DateTimeOffset? LastStockTake { get; set; }
        public int? DaysSinceLastStockTake { get; set; }
        public List<StockTakeSnapshotDto> StockTakes { get; set; } = new();
    }
}
