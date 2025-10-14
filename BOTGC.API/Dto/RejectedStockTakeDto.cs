namespace BOTGC.API.Dto
{
    public sealed class RejectedStockTakeDto
    {
        public int StockItemId { get; init; }
        public string Reason { get; init; } = string.Empty;
        public string? Name { get; init; }
        public string? Division { get; init; }
        public DateTimeOffset At { get; init; }
        public decimal? ObservedQuantity { get; init; }
        public decimal? EstimatedQuantity { get; init; }
        public decimal? TolerancePercent { get; init; }

        public static RejectedStockTakeDto NoObservations(StockTakeEntryDto e, DateTimeOffset at) =>
            new RejectedStockTakeDto { StockItemId = e.StockItemId, Name = e.Name, Division = e.Division, Reason = "No observations recorded.", At = at };

        public static RejectedStockTakeDto MissingItemConfig(StockTakeEntryDto e, DateTimeOffset at) =>
            new RejectedStockTakeDto { StockItemId = e.StockItemId, Name = e.Name, Division = e.Division, Reason = "Missing stock item configuration.", At = at };

        public static RejectedStockTakeDto IncompleteObservations(StockTakeEntryDto e, DateTimeOffset at) =>
            new RejectedStockTakeDto { StockItemId = e.StockItemId, Name = e.Name, Division = e.Division, Reason = "Incomplete observations.", At = at };

        public static RejectedStockTakeDto OutOfTolerance(StockTakeEntryDto e, DateTimeOffset at, decimal observed, decimal estimate, decimal tol) =>
            new RejectedStockTakeDto { StockItemId = e.StockItemId, Name = e.Name, Division = e.Division, Reason = "Observed value outside tolerance.", At = at, ObservedQuantity = observed, EstimatedQuantity = estimate, TolerancePercent = tol };

        public static RejectedStockTakeDto BackendPersistFailed(int stockItemId, DateTimeOffset at) =>
            new RejectedStockTakeDto { StockItemId = stockItemId, Reason = "Failed to persist to backend.", At = at };
    }
}
