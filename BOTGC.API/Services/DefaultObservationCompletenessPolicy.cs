using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;

namespace BOTGC.API.Services
{
    public sealed class DefaultObservationCompletenessPolicy : IObservationCompletenessPolicy
    {
        public bool IsComplete(StockTakeEntryDto entry, StockItemConfig cfg)
        {
            var obs = entry.Observations ?? new List<StockTakeObservationDto>();
            if (obs.Count == 0) return false;

            var hasCount = obs.Any(o => o.Code.StartsWith("Count", System.StringComparison.OrdinalIgnoreCase));
            var hasWeight = obs.Any(o => o.Code.EndsWith("WeightGrams", System.StringComparison.OrdinalIgnoreCase));

            return cfg.Dimension switch
            {
                MeasurementDimension.Count => hasCount,
                MeasurementDimension.Weight => hasCount || hasWeight,
                MeasurementDimension.Volume => hasCount || hasWeight || !cfg.RequiresWeightForOpened,
                _ => hasCount || hasWeight
            };
        }
    }
}
