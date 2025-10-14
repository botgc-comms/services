namespace BOTGC.API.Models
{
    public sealed class BottleWeightRecord
    {
        public int StockItemId { get; set; }
        public decimal EmptyWeightGrams { get; set; }
        public decimal FullWeightGrams { get; set; }
        public decimal NominalVolumeMl { get; set; }
    }
}
