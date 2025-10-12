namespace BOTGC.POS.Models
{
    public sealed class StockTakeViewModel
    {
        public IReadOnlyList<Operator> Operators { get; set; } = Array.Empty<Operator>();
        public bool IsDue { get; init; }
    }

    public sealed class StockTakeDraftEntry
    {
        public int StockItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;

        public Guid OperatorId { get; set; }
        public string OperatorName { get; set; } = string.Empty;

        public DateTimeOffset At { get; set; } // when the observation was recorded

        public List<StockTakeObservation> Observations { get; set; } = new();

        public decimal EstimatedQuantityAtCapture { get; set; }
    }

    public sealed class StockTakeObservation
    {
        public int StockItemId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string? Location { get; set; }
        public decimal Value { get; set; }
    }

    //public sealed class StockTakeCommitDto
    //{
    //    public DateTimeOffset Timestamp { get; set; }
    //    public List<StockTakeObservation> Observations { get; set; } = new();
    //}

    public sealed class StockTakeDivisionPlan
    {
        public string Division { get; set; } = string.Empty;
        public List<StockTakeProduct> Products { get; set; } = new();
    }

    public sealed class StockTakeProduct
    {
        public int StockItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;
        public decimal? CurrentQuantity { get; set; }
        public DateTimeOffset? LastStockTake { get; set; }
        public int? DaysSinceLastStockTake { get; set; }
        public List<StockTakeSnapshot> StockTakes { get; set; } = new();
    }

    public sealed class StockTakeSnapshot
    {
        public DateTimeOffset Timestamp { get; set; }
        public decimal? Before { get; set; }
        public decimal? After { get; set; }
        public decimal? Adjustment { get; set; }
        public int? StockRoomId { get; set; }
    }
}

