namespace BOTGC.API.Dto
{
    public sealed record BottleCalibrationDto(
        int NominalVolumeMl,
        int EmptyWeightGrams,
        int FullWeightGrams,
        double Confidence,
        string Strategy
    );
}
