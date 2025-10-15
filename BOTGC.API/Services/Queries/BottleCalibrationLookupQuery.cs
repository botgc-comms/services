using BOTGC.API.Dto;

namespace BOTGC.API.Services.Queries
{
    public sealed record BottleCalibrationLookupQuery(int StockItemId, string Name, string Division)
    : QueryBase<BottleCalibrationLookupResultDto>;
}
