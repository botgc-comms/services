using System.Linq;
using System.Text.RegularExpressions;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class ProcessStockTakeSheetHandler(
    IOptions<AppSettings> settings,
    ILogger<ProcessStockTakeSheetHandler> logger,
    IMediator mediator,
    IBottleVolumeService bottleVolumeService,
    IQueueService<StockTakeCompletedCommand> completedQueue
) : QueryHandlerBase<ProcessStockTakeCommand, bool>
{
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<ProcessStockTakeSheetHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly IBottleVolumeService _bottleVolumeService = bottleVolumeService ?? throw new ArgumentNullException(nameof(bottleVolumeService));
    private readonly IQueueService<StockTakeCompletedCommand> _completedQueue = completedQueue ?? throw new ArgumentNullException(nameof(completedQueue));

    static decimal SumCounts(IEnumerable<StockTakeObservationDto> obs) =>
    obs.Where(o => o.Code.StartsWith("CountIn", StringComparison.OrdinalIgnoreCase)).Sum(o => o.Value);

    static decimal PercentDelta(decimal observed, decimal estimate) =>
        estimate == 0m ? 0m : ((observed - estimate) / estimate) * 100m;

    public async override Task<bool> Handle(ProcessStockTakeCommand request, CancellationToken cancellationToken)
    {
        int? stockTakeId = null;

        var sheet = request.Sheet;
        var sheetDate = sheet.Date;

        var include = new Dictionary<int, decimal>();
        var notes = new List<string>();
        var rejects = new List<RejectedStockTakeDto>();

        var investigateItems = new List<StockTakeItemInvestigationDto>();
        var acceptedItems = new List<StockTakeItemAcceptedDto>();

        foreach (var entry in sheet.Entries ?? Enumerable.Empty<StockTakeEntryDto>())
        {
            var (dimension, reportingUnit, unitsPerCountInBase) = DeriveFactors(entry);

            if (!HasAnyObservation(entry))
            {
                var msg = $"[{entry.StockItemId}] {Safe(entry.Name)} — No observations captured (neither Count nor Weight).";
                notes.Add(msg);
                _logger.LogWarning("StockTake: {Message}", msg);
                rejects.Add(RejectedStockTakeDto.IncompleteObservations(entry, sheetDate));

                investigateItems.Add(new StockTakeItemInvestigationDto(
                    StockItemId: entry.StockItemId,
                    StockTakeEntry: entry,
                    Message: msg,
                    Observed: 0m,
                    Estimate: entry.EstimatedQuantityAtCapture,
                    Difference: 0m - entry.EstimatedQuantityAtCapture,
                    Percent: PercentDelta(0m, entry.EstimatedQuantityAtCapture),
                    Reason: "No observations",
                    Allowed: null,
                    DominantRule: null
                ));

                continue;
            }

            var hasWeight = HasWeightObservation(entry);

            if (dimension == MeasurementDimension.Volume && hasWeight && unitsPerCountInBase <= 1m)
            {
                var msg = $"[{entry.StockItemId}] {Safe(entry.Name)} — Weight provided but item size is unknown (cannot convert weight to volume).";
                notes.Add(msg);
                _logger.LogWarning("StockTake: {Message}", msg);
                rejects.Add(RejectedStockTakeDto.IncompleteObservations(entry, sheetDate));

                var obs = entry.Observations ?? new List<StockTakeObservationDto>();
                var observedCounts = SumCounts(obs);
                var est = entry.EstimatedQuantityAtCapture;

                investigateItems.Add(new StockTakeItemInvestigationDto(
                    StockItemId: entry.StockItemId,
                    StockTakeEntry: entry,
                    Message: msg,
                    Observed: observedCounts,
                    Estimate: est,
                    Difference: observedCounts - est,
                    Percent: PercentDelta(observedCounts, est),
                    Reason: "Partial / incomplete observations",
                    Allowed: null,
                    DominantRule: null
                ));

                continue;
            }

            decimal observed;
            try
            {
                observed = await ComputeObservedAsync(entry, dimension, reportingUnit, unitsPerCountInBase, cancellationToken);
            }
            catch (BottleVolumeConversionUnavailableException ex)
            {
                var msg = $"[{entry.StockItemId}] {Safe(entry.Name)} — Weight provided but bottle/keg weight profile is missing or invalid (cannot value partial).";
                notes.Add(msg);
                _logger.LogWarning(ex, "StockTake: {Message}", msg);
                rejects.Add(RejectedStockTakeDto.IncompleteObservations(entry, sheetDate));

                var obs = entry.Observations ?? new List<StockTakeObservationDto>();
                var observedCounts = SumCounts(obs);
                var est = entry.EstimatedQuantityAtCapture;

                investigateItems.Add(new StockTakeItemInvestigationDto(
                    StockItemId: entry.StockItemId,
                    StockTakeEntry: entry,
                    Message: msg,
                    Observed: observedCounts,
                    Estimate: est,
                    Difference: observedCounts - est,
                    Percent: PercentDelta(observedCounts, est),
                    Reason: "Partial / incomplete observations",
                    Allowed: null,
                    DominantRule: null
                ));

                continue;
            }

            if (observed < 0m) observed = 0m;

            var baseline = entry.EstimatedQuantityAtCapture;
            var since = entry.At == default ? sheetDate : entry.At;

            var txQuery = new GetStockItemTransactionSinceDateQuery
            {
                StockItemId = entry.StockItemId,
                FromDate = since
            };

            var txns = await _mediator.Send(txQuery, cancellationToken);

            var delta = 0m;
            foreach (var t in txns)
            {
                var action = (t.Action ?? string.Empty).Trim().ToLowerInvariant();
                var quantity = t.Difference ?? 0m;
                switch (action)
                {
                    case "sale":
                    case "wastage":
                        delta -= quantity;
                        break;
                    case "delivery":
                        delta += quantity;
                        break;
                    default:
                        break;
                }
            }

            var adjustedEstimate = baseline + delta;
            var tol = _settings.StockTake?.TolerancePercent ?? 10m;

            var decision = SmartTolerance(
                observed: observed,
                estimate: adjustedEstimate,
                tolPercent: tol,
                absBandSmall: 1m,
                smallEstimateThreshold: 2m,
                poissonK: 2m,
                countGranularity: 1m
            );

            if (!decision.WithinTolerance)
            {
                var reason =
                    !HasAnyObservation(entry) ? "No observations" :
                    "Outside tolerance";

                var msg = $"[{entry.StockItemId}] {Safe(entry.Name)} — {reason}: Observed={observed:0.##} vs Estimate={adjustedEstimate:0.##} (Δ={decision.Difference:0.##}, allowed={decision.Allowed:0.##}, rule={decision.DominantRule}).";
                notes.Add(msg);
                _logger.LogInformation("StockTake: {Message}", msg);
                rejects.Add(RejectedStockTakeDto.OutOfTolerance(entry, sheetDate, observed, adjustedEstimate, tol));

                investigateItems.Add(new StockTakeItemInvestigationDto(
                    StockItemId: entry.StockItemId,
                    StockTakeEntry: entry,
                    Message: msg,
                    Observed: observed,
                    Estimate: adjustedEstimate,
                    Difference: decision.Difference,
                    Percent: adjustedEstimate == 0m ? 0m : (observed - adjustedEstimate) / adjustedEstimate * 100m,
                    Reason: reason,
                    Allowed: decision.Allowed,
                    DominantRule: decision.DominantRule
                ));
                continue;
            }

            include[entry.StockItemId] = observed;
        }

        if (include.Count > 0)
        {
            // Choose a sensible time for the stock-take:
            // Use the latest non-default 'At' from the INCLUDED entries; otherwise 09:00 local on the sheet date.
            var includedEntries = sheet.Entries.Where(e => include.ContainsKey(e.StockItemId)).ToList();
            var nonDefaultTimes = includedEntries.Select(e => e.At).Where(a => a != default).ToList();

            DateTime takenAtLocalFinal;
            if (nonDefaultTimes.Count > 0)
            {
                var latest = nonDefaultTimes.Max().ToLocalTime();
                takenAtLocalFinal = new DateTime(sheetDate.Year, sheetDate.Month, sheetDate.Day, latest.Hour, latest.Minute, latest.Second, DateTimeKind.Local);
            }
            else
            {
                takenAtLocalFinal = new DateTime(sheetDate.Year, sheetDate.Month, sheetDate.Day, 9, 0, 0, DateTimeKind.Local);
            }

            // Provide a per-item reason for the UI (“Reason” column)
            var reasons = new Dictionary<int, string>();
            foreach (var e in includedEntries)
            {
                var who = string.IsNullOrWhiteSpace(e.OperatorName) ? "operator" : e.OperatorName.Trim();
                reasons[e.StockItemId] = $"Mini stock take performed by {who}";
            }

            var saveCmd = new SaveStockTakeCommand(takenAtLocalFinal, include, reasons);
            var ok = await _mediator.Send(saveCmd, cancellationToken);
            if (!ok.HasValue)
            {
                foreach (var kv in include)
                {
                    // kv.Key = stockItemId, kv.Value = observed
                    var e = sheet.Entries.First(x => x.StockItemId == kv.Key);

                    var failMsg = $"[{kv.Key}] Persist failed in backend; item excluded.";
                    notes.Add(failMsg);
                    _logger.LogError("StockTake: {Message}", failMsg);
                    rejects.Add(RejectedStockTakeDto.BackendPersistFailed(kv.Key, sheetDate));

                    var baseline = e.EstimatedQuantityAtCapture;
                    var observedNow = kv.Value;

                    investigateItems.Add(new StockTakeItemInvestigationDto(
                        StockItemId: kv.Key,
                        StockTakeEntry: e,
                        Message: failMsg,
                        Observed: observedNow,
                        Estimate: baseline,
                        Difference: observedNow - baseline,
                        Percent: PercentDelta(observedNow, baseline),
                        Reason: "Backend persist failed",
                        Allowed: null,
                        DominantRule: null
                    ));
                }

                include.Clear();
            }
            else
            {
                stockTakeId = ok.Value;

                foreach (var kv in include)
                {
                    var e = sheet.Entries.First(x => x.StockItemId == kv.Key);
                    var who = string.IsNullOrWhiteSpace(e.OperatorName) ? "operator" : e.OperatorName.Trim();
                    var okMsg = $"Accepted within tolerance. Mini stock take performed by {who}.";
                    var est = e.EstimatedQuantityAtCapture; // baseline
                    var since = e.At == default ? sheetDate : e.At;

                    var adjusted = est; 

                    acceptedItems.Add(new StockTakeItemAcceptedDto(
                        StockItemId: kv.Key,
                        StockTakeEntry: e,
                        Message: okMsg,
                        Observed: kv.Value,
                        Estimate: adjusted,
                        Difference: kv.Value - adjusted,
                        Percent: PercentDelta(kv.Value, adjusted)
                    ));
                }
            }
        }

        var header = $"StockTake {request.Date:yyyy-MM-dd} / {request.Division}: accepted={include.Count}, investigate={rejects.Count}.";
        var details = notes.Count == 0 ? "No issues detected." : string.Join("\n", notes.Select(n => $" - {n}"));
        var summary = $"{header}\n{details}";

        var completionTicket = new StockTakeCompletedCommand(
            StockTakeId: stockTakeId,
            Date: request.Date,
            Division: request.Division,
            InvestigateItems: investigateItems,
            AcceptedItems: acceptedItems,
            CorrelationId: Guid.NewGuid().ToString("N"),
            Summary: summary
        );

        await _completedQueue.EnqueueAsync(completionTicket, cancellationToken);

        return true;
    }

    private static bool HasAnyObservation(StockTakeEntryDto entry)
    {
        var obs = entry.Observations ?? new List<StockTakeObservationDto>();
        return obs.Count > 0;
    }

    private static bool HasWeightObservation(StockTakeEntryDto entry)
    {
        var obs = entry.Observations ?? new List<StockTakeObservationDto>();
        return obs.Any(o =>
            o.Code.EndsWith("WeightGrams", StringComparison.OrdinalIgnoreCase) &&
            o.Value > 0m);
    }

    private static string Safe(string? s) => string.IsNullOrWhiteSpace(s) ? "(unnamed)" : s.Trim();

    private async Task<decimal> ComputeObservedAsync(
        StockTakeEntryDto entry,
        MeasurementDimension dimension,
        StockUnit reportingUnit,
        decimal unitsPerCountInBase,
        CancellationToken ct)
    {
        var obs = entry.Observations ?? new List<StockTakeObservationDto>();

        var countTotal = obs
            .Where(o => o.Code.StartsWith("Count", StringComparison.OrdinalIgnoreCase))
            .Sum(o => o.Value);

        var weightObs = obs
            .Where(o => o.Code.EndsWith("WeightGrams", StringComparison.OrdinalIgnoreCase) && o.Value > 0m)
            .ToList();

        decimal totalBase = 0m;

        if (countTotal != 0m)
        {
            totalBase += countTotal * unitsPerCountInBase;
        }

        if (weightObs.Count > 0)
        {
            switch (dimension)
            {
                case MeasurementDimension.Weight:
                    {
                        totalBase += weightObs.Sum(w => w.Value); // grams
                        break;
                    }
                case MeasurementDimension.Volume:
                    {
                        decimal mlSum = 0m;
                        foreach (var w in weightObs)
                        {
                            decimal ml;
                            try
                            {
                                ml = await _bottleVolumeService.ToVolumeMlAsync(entry.StockItemId, w.Value, ct); // per obs
                            }
                            catch
                            {
                                throw new BottleVolumeConversionUnavailableException(
                                    $"Weight-to-volume conversion unavailable for stock item {entry.StockItemId} ({entry.Name}).");
                            }

                            if (ml < 0m)
                            {
                                throw new BottleVolumeConversionUnavailableException(
                                    $"Weight-to-volume conversion returned a negative volume for stock item {entry.StockItemId} ({entry.Name}).");
                            }

                            mlSum += ml;
                        }
                        totalBase += mlSum; // ml
                        break;
                    }
                case MeasurementDimension.Count:
                    {
                        break;
                    }
            }
        }

        decimal result = dimension switch
        {
            MeasurementDimension.Count => totalBase,

            MeasurementDimension.Volume => reportingUnit switch
            {
                StockUnit.Bottle or StockUnit.Can or StockUnit.Splash
                    => Divide(totalBase, unitsPerCountInBase), // ml → bottles/cans/splashes
                StockUnit.Pint => Divide(totalBase, 568m),
                StockUnit.Litre => Divide(totalBase, 1000m),
                StockUnit.Millilitre => totalBase,
                _ => totalBase
            },

            MeasurementDimension.Weight => reportingUnit switch
            {
                StockUnit.Kilogram => Divide(totalBase, 1000m), // g → kg
                StockUnit.Gram => totalBase,
                StockUnit.Packet => totalBase,
                _ => totalBase
            },

            _ => totalBase
        };

        return Math.Round(result, 2, MidpointRounding.AwayFromZero);
    }

    private static decimal Divide(decimal n, decimal d) => d <= 0m ? 0m : n / d;

    private (MeasurementDimension Dimension, StockUnit Reporting, decimal UnitsPerCountInBase) DeriveFactors(StockTakeEntryDto entry)
    {
        var unit = (entry.Unit ?? string.Empty).Trim().ToLowerInvariant();

        var dimension = MapDimension(unit);
        var reporting = MapReportingUnit(unit);

        if (TryParseSizeFromName(entry.Name, out var inferredDim, out var perCountBase))
        {
            if (dimension == MeasurementDimension.Count || dimension == inferredDim)
            {
                dimension = inferredDim;
                return (dimension, reporting, perCountBase);
            }
        }

        return unit switch
        {
            "pint" => (MeasurementDimension.Volume, StockUnit.Pint, 568m),
            "litre" or "l" => (MeasurementDimension.Volume, StockUnit.Litre, 1000m),
            "kg" or "kilogram" => (MeasurementDimension.Weight, StockUnit.Kilogram, 1000m),

            "bottle" => (MeasurementDimension.Volume, StockUnit.Bottle, 1m),
            "can" => (MeasurementDimension.Volume, StockUnit.Can, 1m),
            "splash" => (MeasurementDimension.Volume, StockUnit.Splash, 1m),
            "packet" => (MeasurementDimension.Weight, StockUnit.Packet, 1m),

            _ => (MeasurementDimension.Count, StockUnit.Each, 1m)
        };
    }

    public static ToleranceDecision SmartTolerance(
        decimal observed,
        decimal estimate,
        decimal tolPercent = 10m,
        decimal absBandSmall = 1m,
        decimal smallEstimateThreshold = 2m,
        decimal poissonK = 2m,
        decimal countGranularity = 1m,
        decimal? unitValue = null,
        decimal valueBandMoney = 0m,
        decimal minBand = 0m,
        decimal maxBand = 999999m
    )
    {
        var diffRaw = Math.Abs(observed - estimate);
        decimal diff = SnapToGranularity(diffRaw, countGranularity);

        decimal bandAbs = estimate <= smallEstimateThreshold ? absBandSmall : 0m;
        decimal bandPct = estimate > 0m ? estimate * (tolPercent / 100m) : 0m;

        decimal bandPoisson = 0m;
        if (estimate > 0m)
        {
            var sqrt = (decimal)Math.Sqrt((double)estimate);
            bandPoisson = poissonK * sqrt;
        }

        decimal bandValue = 0m;
        if (unitValue.HasValue && unitValue.Value > 0m && valueBandMoney > 0m)
        {
            bandValue = valueBandMoney / unitValue.Value;
        }

        var bands = new (string Name, decimal Value)[]
        {
            ("absolute-small", bandAbs),
            ("percent", bandPct),
            ("poisson", bandPoisson),
            ("value", bandValue),
            ("min", minBand)
        };

        var dominant = bands.OrderByDescending(b => b.Value).First();
        var allowedRaw = Math.Min(Math.Max(dominant.Value, 0m), maxBand);
        var allowed = SnapToGranularity(allowedRaw, countGranularity);

        var within = diff <= allowed;

        var notes = string.Join(", ",
            bands.Where(b => b.Value > 0m).Select(b => $"{b.Name}={b.Value:0.##}"))
            .Trim();

        return new ToleranceDecision(within, diff, allowed, dominant.Name, string.IsNullOrWhiteSpace(notes) ? "none" : notes);

        static decimal SnapToGranularity(decimal v, decimal step)
        {
            if (step <= 0m) return v;
            var n = Math.Round(v / step, MidpointRounding.AwayFromZero);
            return n * step;
        }
    }

    private static MeasurementDimension MapDimension(string unit) => unit switch
    {
        "kg" or "kilogram" or "gram" or "g" or "packet" => MeasurementDimension.Weight,
        "pint" or "ml" or "millilitre" or "litre" or "l" or "bottle" or "can" or "splash" => MeasurementDimension.Volume,
        _ => MeasurementDimension.Count
    };

    private static StockUnit MapReportingUnit(string unit) => unit switch
    {
        "pint" => StockUnit.Pint,
        "ml" or "millilitre" => StockUnit.Millilitre,
        "litre" or "l" => StockUnit.Litre,
        "kg" or "kilogram" => StockUnit.Kilogram,
        "gram" or "g" => StockUnit.Gram,
        "packet" => StockUnit.Packet,
        "can" => StockUnit.Can,
        "bottle" => StockUnit.Bottle,
        "splash" => StockUnit.Splash,
        _ => StockUnit.Each
    };

    private static readonly Regex VolumeRx = new(
        @"(?<!\d)(?<v>\d+(?:\.\d+)?)\s*(?<u>ml|millilitres?|cl|l|litres?|pint|pints)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex WeightRx = new(
        @"(?<!\d)(?<v>\d+(?:\.\d+)?)\s*(?<u>g|grams?|kg|kilograms?)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static bool TryParseSizeFromName(string? name, out MeasurementDimension dim, out decimal perCountBase)
    {
        dim = MeasurementDimension.Count;
        perCountBase = 0m;
        if (string.IsNullOrWhiteSpace(name)) return false;

        var text = name.ToLowerInvariant();

        var w = WeightRx.Match(text);
        if (w.Success && decimal.TryParse(w.Groups["v"].Value, out var wVal))
        {
            dim = MeasurementDimension.Weight;
            perCountBase = w.Groups["u"].Value.StartsWith("kg", StringComparison.Ordinal) ? wVal * 1000m : wVal;
            return true;
        }

        var v = VolumeRx.Match(text);
        if (v.Success && decimal.TryParse(v.Groups["v"].Value, out var vVal))
        {
            dim = MeasurementDimension.Volume;
            perCountBase = v.Groups["u"].Value switch
            {
                "l" or "litre" or "litres" => vVal * 1000m,
                "cl" => vVal * 10m,
                "pint" or "pints" => vVal * 568m,
                _ => vVal
            };
            return true;
        }

        return false;
    }

    private sealed class BottleVolumeConversionUnavailableException : Exception
    {
        public BottleVolumeConversionUnavailableException(string message) : base(message) { }
    }

    private enum MeasurementDimension { Count = 1, Volume = 2, Weight = 3 }
    private enum StockUnit { Each = 1, Can = 2, Bottle = 3, Pint = 4, Packet = 5, Splash = 6, Millilitre = 7, Litre = 8, Gram = 9, Kilogram = 10 }
}

public sealed record ToleranceDecision(
    bool WithinTolerance,
    decimal Difference,
    decimal Allowed,
    string DominantRule,
    string Notes
);
