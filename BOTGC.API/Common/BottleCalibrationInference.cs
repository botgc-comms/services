using System.Text.RegularExpressions;

namespace BOTGC.API.Common
{
    public static class BottleCalibrationInference
    {
        // Conservative defaults; adjust anytime centrally.
        private static readonly Dictionary<string, decimal> DensityByCategory = new(StringComparer.OrdinalIgnoreCase)
        {
            ["wine"] = 0.99m,
            ["spirits"] = 0.94m,
            ["liqueur"] = 1.06m,
            ["cordial"] = 1.30m,
            ["beer"] = 1.01m,
            ["minerals"] = 1.00m,
            ["water"] = 1.00m
        };

        public static decimal GuessDensity(string division, string name)
        {
            var s = $"{division} {name}".ToLowerInvariant();
            if (s.Contains("wine")) return DensityByCategory["wine"];
            if (s.Contains("gin") || s.Contains("vodka") || s.Contains("rum") || s.Contains("whisk")) return DensityByCategory["spirits"];
            if (s.Contains("spirit")) return DensityByCategory["spirits"];
            if (s.Contains("liqueur") || s.Contains("baileys") || s.Contains("amaretto")) return DensityByCategory["liqueur"];
            if (s.Contains("cordial") || s.Contains("syrup")) return DensityByCategory["cordial"];
            if (s.Contains("beer") || s.Contains("draught") || s.Contains("lager") || s.Contains("ale")) return DensityByCategory["beer"];
            if (s.Contains("mineral") || s.Contains("water")) return DensityByCategory["minerals"];
            return DensityByCategory["water"];
        }

        public static int? InferMissingWeight(int? fullG, int? emptyG, int? nominalMl, decimal? densityGPerMl)
        {
            if (!nominalMl.HasValue || !densityGPerMl.HasValue) return null;
            var liquidMass = (int)Math.Round(nominalMl.Value * densityGPerMl.Value, MidpointRounding.AwayFromZero);
            if (fullG.HasValue && !emptyG.HasValue) return fullG.Value - liquidMass;   // inferred empty
            if (emptyG.HasValue && !fullG.HasValue) return emptyG.Value + liquidMass; // inferred full
            return null;
        }

        public static int? InferNominalFromName(string name, string division)
        {
            var s = (name ?? string.Empty).ToLowerInvariant();

            var ml = Regex.Match(s, @"(\d+(?:[.,]\d+)?)\s*ml\b");
            if (ml.Success) return (int)Math.Round(decimal.Parse(ml.Groups[1].Value.Replace(',', '.')));

            var cl = Regex.Match(s, @"(\d+(?:[.,]\d+)?)\s*cl\b");
            if (cl.Success) return (int)Math.Round(decimal.Parse(cl.Groups[1].Value.Replace(',', '.')) * 10m);

            var l = Regex.Match(s, @"(\d+(?:[.,]\d+)?)\s*l\b");
            if (l.Success) return (int)Math.Round(decimal.Parse(l.Groups[1].Value.Replace(',', '.')) * 1000m);

            var d = (division ?? string.Empty).ToLowerInvariant();
            if (d.Contains("wine")) return 750;
            if (d.Contains("spirits")) return 700;     // UK standard
            if (d.Contains("liqueur")) return 500;
            if (d.Contains("cordial")) return 1000;
            if (d.Contains("beer")) return 500;
            if (d.Contains("minerals")) return 500;

            return 750;
        }
    }

}
