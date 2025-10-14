using BOTGC.API.Dto;
using BOTGC.API.Interfaces;

namespace BOTGC.API.Services
{
    public sealed class BottleVolumeService : IBottleVolumeService
    {
        private readonly IBottleWeightDataSource _data;
        private const decimal Epsilon = 5m; // grams tolerance for scale noise

        public BottleVolumeService(IBottleWeightDataSource data)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
        }

        public async Task<decimal> ToVolumeMlAsync(int stockItemId, decimal grossWeightGrams, CancellationToken ct)
        {
            if (grossWeightGrams <= 0m) return 0m;

            var profile = await _data.GetAsync(stockItemId, ct);
            if (profile is null)
            {
                throw new InvalidOperationException($"No bottle weight profile configured for stock item {stockItemId}.");
            }

            var empty = profile.EmptyWeightGrams;
            var full = profile.FullWeightGrams;
            var nominalMl = profile.NominalVolumeMl;

            if (empty <= 0m || full <= empty || nominalMl <= 0m)
            {
                throw new InvalidOperationException($"Invalid bottle weight profile for stock item {stockItemId}.");
            }

            // Reject clearly inconsistent readings
            if (grossWeightGrams < empty - Epsilon)
            {
                throw new InvalidOperationException($"Gross weight {grossWeightGrams}g is below empty weight {empty}g for stock item {stockItemId}.");
            }

            if (grossWeightGrams > full + Epsilon)
            {
                throw new InvalidOperationException($"Gross weight {grossWeightGrams}g is above full weight {full}g for stock item {stockItemId}.");
            }

            // True empty within tolerance → return 0 ml (valid zero)
            if (Math.Abs(grossWeightGrams - empty) <= Epsilon)
            {
                return 0m;
            }

            // Compute contents weight and scale to ml
            var contentsWeight = grossWeightGrams - empty;
            var fullContentsWeight = full - empty;

            if (contentsWeight <= 0m)
            {
                // Should have been caught by epsilon check above; treat as empty
                return 0m;
            }

            var fractionFull = contentsWeight / fullContentsWeight;
            if (fractionFull < 0m) fractionFull = 0m;
            if (fractionFull > 1m) fractionFull = 1m;

            var ml = fractionFull * nominalMl;

            // Normalise tiny negatives/float noise
            if (ml < 0.0001m) ml = 0m;

            return Math.Round(ml, 2, MidpointRounding.AwayFromZero);
        }
    }
}
