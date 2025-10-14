namespace BOTGC.API.Models
{
    public sealed class StockItemConfig
    {
        public int StockItemId { get; set; }
        public MeasurementDimension Dimension { get; set; } = MeasurementDimension.Count;
        public StockUnit ReportingUnit { get; set; } = StockUnit.Each;
        public decimal UnitsPerCountInBaseUnits { get; set; } = 1m;
        public decimal? MlPerReportingUnit { get; set; }
        public decimal? GramsPerReportingUnit { get; set; }
        public bool RequiresWeightForOpened { get; set; } = false;
    }
}
