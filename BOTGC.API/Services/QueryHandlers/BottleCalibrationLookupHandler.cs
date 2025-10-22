using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;
using MediatR;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Text;
using BOTGC.API.Dto;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class BottleCalibrationLookupHandler(ITableStore<BottleCalibrationEntity> store) : QueryHandlerBase<BottleCalibrationLookupQuery, BottleCalibrationLookupResultDto>
{
    private static readonly Regex MlRegex = new(@"(\d+(?:[.,]\d+)?)\s*ml\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex ClRegex = new(@"(\d+(?:[.,]\d+)?)\s*cl\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex LRegex = new(@"(\d+(?:[.,]\d+)?)\s*l\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
        {
            "the","a","an","and","of","with","&","bottle","ml","cl","l","litre","liter",
            "gin","vodka","rum","whisky","whiskey","tequila","brandy","liqueur","cordial","syrup","spirit",
            "rose","rosé","red","white","wine","beer","lager","ale","stout","cider",
            "zero","low","no","alcohol","percent","abv"
        };

    private readonly ITableStore<BottleCalibrationEntity> _store = store;

    public override async Task<BottleCalibrationLookupResultDto> Handle(BottleCalibrationLookupQuery request, CancellationToken cancellationToken)
    {
        var exact = await _store.GetAsync("BottleCalibration", BottleCalibrationEntity.RowKeyFor(request.StockItemId), cancellationToken);
        if (exact is not null)
        {
            return new BottleCalibrationLookupResultDto
            {
                Entity = exact,
                Confidence = 1.0,
                Strategy = "Exact"
            };
        }

        var targetName = request.Name ?? string.Empty;
        var targetDivision = request.Division ?? string.Empty;

        var targetNormal = NormaliseName(targetName);
        var targetTokens = Tokens(targetNormal);
        var targetBrand = BrandKey(targetTokens);
        var targetType = LiquidTypeKey(targetDivision, targetName);
        var targetVol = ParseMlFromName(targetName) ?? DefaultNominalMlByDivision(targetDivision);

        var candidates = new List<BottleCalibrationEntity>();
        await foreach (var e in _store.QueryByPartitionAsync("BottleCalibration", null, cancellationToken))
        {
            candidates.Add(e);
        }

        if (candidates.Count == 0)
        {
            return new BottleCalibrationLookupResultDto
            {
                Entity = null,
                Confidence = 0.0,
                Strategy = "None"
            };
        }

        var scored = candidates
            .Select(e =>
            {
                var eName = e.Name ?? string.Empty;
                var eDiv = e.Division ?? string.Empty;

                var eNormal = NormaliseName(eName);
                var eTokens = Tokens(eNormal);
                var eBrand = BrandKey(eTokens);
                var eType = LiquidTypeKey(eDiv, eName);
                var eVol = e.NominalVolumeMl;

                var brandScore = Similarity(targetBrand, eBrand);
                var typeScore = targetType.Equals(eType, StringComparison.OrdinalIgnoreCase) ? 1.0 : 0.0;
                var volScore = VolumeScore(targetVol, eVol);
                var tokenJaccard = Jaccard(targetTokens, eTokens);

                var score =
                    0.40 * brandScore +
                    0.30 * volScore +
                    0.20 * typeScore +
                    0.10 * tokenJaccard;

                return (Entity: e, Score: Clamp01(score));
            })
            .OrderByDescending(x => x.Score)
            .ToList();

        var best = scored.First();

        return new BottleCalibrationLookupResultDto
        {
            Entity = best.Entity,
            Confidence = best.Score,
            Strategy = best.Score >= 0.95 ? "HighConfidence" : best.Score >= 0.80 ? "Probable" : "LowConfidence"
        };
    }

    private static string NormaliseName(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return string.Empty;
        var nfkd = s.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(nfkd.Length);
        foreach (var ch in nfkd)
        {
            var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (uc != UnicodeCategory.NonSpacingMark) sb.Append(ch);
        }
        var ascii = sb.ToString().Normalize(NormalizationForm.FormC);
        ascii = ascii.ToLowerInvariant();
        ascii = Regex.Replace(ascii, @"[^a-z0-9\s]", " ");
        ascii = Regex.Replace(ascii, @"\s+", " ").Trim();
        return ascii;
    }

    private static List<string> Tokens(string s)
    {
        return s.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(t => !StopWords.Contains(t) && !Regex.IsMatch(t, @"^\d+$"))
                .ToList();
    }

    private static string BrandKey(List<string> tokens)
    {
        if (tokens.Count == 0) return string.Empty;
        if (tokens.Count == 1) return tokens[0];
        return string.Join(" ", tokens.Take(2));
    }

    private static string LiquidTypeKey(string division, string name)
    {
        var s = $"{division} {name}".ToLowerInvariant();
        if (s.Contains("gin")) return "gin";
        if (s.Contains("vodka")) return "vodka";
        if (s.Contains("rum")) return "rum";
        if (s.Contains("whisky") || s.Contains("whiskey")) return "whisky";
        if (s.Contains("tequila")) return "tequila";
        if (s.Contains("liqueur") || s.Contains("baileys") || s.Contains("amaretto")) return "liqueur";
        if (s.Contains("cordial") || s.Contains("syrup")) return "cordial";
        if (s.Contains("wine") || s.Contains("merlot") || s.Contains("shiraz") || s.Contains("malbec") || s.Contains("prosecco")) return "wine";
        if (s.Contains("beer") || s.Contains("lager") || s.Contains("ale") || s.Contains("stout")) return "beer";
        if (s.Contains("minerals") || s.Contains("water") || s.Contains("cola") || s.Contains("tango") || s.Contains("pepsi")) return "minerals";
        return "other";
    }

    private static int DefaultNominalMlByDivision(string division)
    {
        var d = division.ToLowerInvariant();
        if (d.Contains("wine")) return 750;
        if (d.Contains("spirits")) return 700;
        if (d.Contains("liqueur")) return 500;
        if (d.Contains("cordial")) return 1000;
        if (d.Contains("beer")) return 500;
        if (d.Contains("minerals")) return 500;
        return 750;
    }

    private static int? ParseMlFromName(string name)
    {
        var s = name.ToLowerInvariant();

        var ml = MlRegex.Match(s);
        if (ml.Success && decimal.TryParse(ml.Groups[1].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var mlVal))
            return (int)Math.Round(mlVal);

        var cl = ClRegex.Match(s);
        if (cl.Success && decimal.TryParse(cl.Groups[1].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var clVal))
            return (int)Math.Round(clVal * 10m);

        var l = LRegex.Match(s);
        if (l.Success && decimal.TryParse(l.Groups[1].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var lVal))
            return (int)Math.Round(lVal * 1000m);

        var multi = Regex.Match(s, @"\b(\d+)\s*[x×]\s*(\d+(?:[.,]\d+)?)\s*ml\b");
        if (multi.Success && decimal.TryParse(multi.Groups[2].Value.Replace(',', '.'), NumberStyles.Number, CultureInfo.InvariantCulture, out var single))
            return (int)Math.Round(single);

        return null;
    }

    private static double VolumeScore(int targetMl, int candidateMl)
    {
        if (targetMl <= 0 || candidateMl <= 0) return 0.0;
        var diff = Math.Abs(targetMl - candidateMl);
        var pct = diff / (double)targetMl;
        if (pct <= 0.02) return 1.0;
        if (pct <= 0.05) return 0.9;
        if (pct <= 0.10) return 0.7;
        if (pct <= 0.20) return 0.4;
        return 0.0;
    }

    private static double Jaccard(List<string> a, List<string> b)
    {
        if (a.Count == 0 || b.Count == 0) return 0.0;
        var setA = a.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var setB = b.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var inter = setA.Intersect(setB, StringComparer.OrdinalIgnoreCase).Count();
        var union = setA.Union(setB, StringComparer.OrdinalIgnoreCase).Count();
        if (union == 0) return 0.0;
        return inter / (double)union;
    }

    private static double Similarity(string a, string b)
    {
        if (string.IsNullOrEmpty(a) && string.IsNullOrEmpty(b)) return 1.0;
        if (string.IsNullOrEmpty(a) || string.IsNullOrEmpty(b)) return 0.0;
        var dist = Levenshtein(a, b);
        var max = Math.Max(a.Length, b.Length);
        if (max == 0) return 1.0;
        return 1.0 - (dist / (double)max);
    }

    private static int Levenshtein(string s, string t)
    {
        var n = s.Length;
        var m = t.Length;
        if (n == 0) return m;
        if (m == 0) return n;
        var d = new int[n + 1, m + 1];
        for (var i = 0; i <= n; i++) d[i, 0] = i;
        for (var j = 0; j <= m; j++) d[0, j] = j;
        for (var i = 1; i <= n; i++)
        {
            for (var j = 1; j <= m; j++)
            {
                var cost = s[i - 1] == t[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }

    private static double Clamp01(double x) => x < 0 ? 0 : x > 1 ? 1 : x;
}