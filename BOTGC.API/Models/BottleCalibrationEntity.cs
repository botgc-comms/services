using Azure.Data.Tables;
using Azure;

namespace BOTGC.API.Models;

public enum BottleMetricSource
{
    Unknown = 0,
    Measured = 1,   // weighed on scales
    Estimated = 2,  // calculated from density/volume or back-calculated from a reading
    Inferred = 3   // copied from a similar product (brand/size), with confidence
}

public sealed partial class BottleCalibrationEntity : ITableEntity
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

    // NEW: provenance flags
    public int EmptyWeightSource { get; set; } = (int)BottleMetricSource.Unknown;
    public int FullWeightSource { get; set; } = (int)BottleMetricSource.Unknown;
    public int NominalVolumeSource { get; set; } = (int)BottleMetricSource.Unknown;

    // NEW: who/when for measured values (optional)
    public Guid? MeasuredByOperatorId { get; set; }
    public DateTimeOffset? MeasuredAtUtc { get; set; }

    // NEW: inference metadata (when this row was created by copying another)
    public int? InferredFromStockItemId { get; set; }
    public string? InferredFromName { get; set; }
    public double? InferredConfidence { get; set; }

    public DateTimeOffset UpdatedAtUtc { get; set; } = DateTimeOffset.UtcNow;

    public static string RowKeyFor(int stockItemId) => stockItemId.ToString();

    public static BottleCalibrationEntity CreateMeasured(
        int stockItemId, string name, string division,
        int nominalMl, int emptyG, int fullG,
        Guid? measuredByOperatorId = null, DateTimeOffset? measuredAtUtc = null)
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
            EmptyWeightSource = (int)BottleMetricSource.Measured,
            FullWeightSource = (int)BottleMetricSource.Measured,
            NominalVolumeSource = (int)BottleMetricSource.Estimated, 
            MeasuredByOperatorId = measuredByOperatorId,
            MeasuredAtUtc = measuredAtUtc ?? DateTimeOffset.UtcNow,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
    }

    public static BottleCalibrationEntity CreateInferred(
        int stockItemId, string name, string division,
        int nominalMl, int emptyG, int fullG,
        int fromStockItemId, string fromName, double confidence)
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
            EmptyWeightSource = (int)BottleMetricSource.Inferred,
            FullWeightSource = (int)BottleMetricSource.Inferred,
            NominalVolumeSource = (int)BottleMetricSource.Estimated,
            InferredFromStockItemId = fromStockItemId,
            InferredFromName = fromName,
            InferredConfidence = confidence,
            UpdatedAtUtc = DateTimeOffset.UtcNow
        };
    }
}