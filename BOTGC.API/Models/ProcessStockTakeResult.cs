namespace BOTGC.API.Models
{
    public sealed class ProcessStockTakeResult
    {
        public int CommittedCount { get; init; }
        public int OutOfToleranceCount { get; init; }
        public IReadOnlyList<int> OutOfToleranceItemIds { get; init; } = Array.Empty<int>();
    }
}
