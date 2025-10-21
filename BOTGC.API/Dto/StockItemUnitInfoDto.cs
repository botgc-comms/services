namespace BOTGC.API.Dto
{
    public sealed record StockItemUnitInfoDto(int UnitId, string UnitName, decimal Cost, decimal? ConversionRatio = null, decimal? PrecisionOfUnits = null);

}
