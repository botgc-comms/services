namespace BOTGC.API.Dto
{
    public sealed record StockTakeEntryDto(
        int StockItemId,
        string Name,
        string Division,
        string Unit,
        Guid OperatorId,
        string OperatorName,
        DateTime At,
        List<StockTakeObservationDto> Observations,
        decimal EstimatedQuantityAtCapture
    )
    {
        public BottleCalibrationDto? Calibration { get; set; }
        public bool CalibrationHighConfidence { get; set; }
    }
}
