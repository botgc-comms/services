namespace BOTGC.API.Dto
{
    public sealed class StockTakeSnapshotDto
    {
        public DateTimeOffset Timestamp { get; set; }
        public decimal? Before { get; set; }
        public decimal? After { get; set; }
        public decimal? Adjustment { get; set; }
    }
}
