namespace BOTGC.API.Dto
{

    public sealed class StockTakeSnapshotDto
    {
        public DateTimeOffset Timestamp { get; set; }
        public decimal? Before { get; set; }
        public decimal? After { get; set; }
        public decimal? Adjustment { get; set; }
    }

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

    public sealed class StockTakeEntryDto : HateoasResource
    {
        public int StockItemId { get; set; }
        public string? Name { get; set; }
        public string? Unit { get; set; }
        public string? Division { get; set; }
        public decimal? PreviousQuantity { get; set; }
        public decimal? NewQuantity { get; set; }
        public decimal? Difference { get; set; }
        public decimal? CurrentQuantity { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public int? StockRoomId { get; set; }
        public int? OperatorId { get; set; }
        public string? OperatorName { get; set; }
        public int? ZRead { get; set; }
    }

}
