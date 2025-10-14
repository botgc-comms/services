namespace BOTGC.API.Dto
{
    public sealed record BottleWeightProfileDto(
       int StockItemId,
       decimal EmptyWeightGrams,
       decimal FullWeightGrams,
       decimal NominalVolumeMl
   );
}
