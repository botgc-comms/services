using Azure.Data.Tables;
using Azure;

namespace BOTGC.API.Models
{
    public sealed class BottleCalibrationEntity : ITableEntity
    {
        public string PartitionKey { get; set; } = "BottleCalibration";
        public string RowKey { get; set; } = string.Empty;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public int StockItemId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Division { get; set; } = string.Empty;

        public int NominalVolumeMl { get; set; }
        public int EmptyWeightGrams { get; set; }
        public int FullWeightGrams { get; set; }

        public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

        public static string RowKeyFor(int stockItemId) => stockItemId.ToString();

        public static BottleCalibrationEntity Create(int stockItemId, string name, string division, int nominalMl, int emptyG, int fullG)
        {
            return new BottleCalibrationEntity
            {
                PartitionKey = "BottleCalibration",
                RowKey = RowKeyFor(stockItemId),
                StockItemId = stockItemId,
                Name = name ?? string.Empty,
                Division = division ?? string.Empty,
                NominalVolumeMl = nominalMl,
                EmptyWeightGrams = emptyG,
                FullWeightGrams = fullG,
                UpdatedAtUtc = DateTimeOffset.UtcNow
            };
        }
    }
}
