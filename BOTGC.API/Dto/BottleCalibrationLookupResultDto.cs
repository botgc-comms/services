using BOTGC.API.Models;

namespace BOTGC.API.Dto
{
    public sealed class BottleCalibrationLookupResultDto
    {
        public BottleCalibrationEntity? Entity { get; init; }
        public double Confidence { get; init; }
        public string Strategy { get; init; } = string.Empty;
        public bool IsHighConfidence => Confidence >= 0.95;
    }
}
