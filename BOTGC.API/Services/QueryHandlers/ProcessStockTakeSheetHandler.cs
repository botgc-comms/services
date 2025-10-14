using System.Text.RegularExpressions;
using System.Linq;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public sealed class ProcessStockTakeSheetHandler(
        IOptions<AppSettings> settings,
        ILogger<ProcessStockTakeSheetHandler> logger,
        IMediator mediator,
        IBottleVolumeService bottleVolumeService,
        IQueueService<StockTakeCompletedTicketCommandDto> completedQueue
    ) : QueryHandlerBase<ProcessStockTakeCommand, bool>
    {
        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<ProcessStockTakeSheetHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly IBottleVolumeService _bottleVolumeService = bottleVolumeService ?? throw new ArgumentNullException(nameof(bottleVolumeService));
        private readonly IQueueService<StockTakeCompletedTicketCommandDto> _completedQueue = completedQueue ?? throw new ArgumentNullException(nameof(completedQueue));

        public async override Task<bool> Handle(ProcessStockTakeCommand request, CancellationToken cancellationToken)
        {
            var sheet = request.Sheet;
            var sheetDate = sheet.Date; // date part
            var include = new Dictionary<int, decimal>();
            var notes = new List<string>();
            var rejects = new List<RejectedStockTakeDto>();

            foreach (var entry in sheet.Entries ?? Enumerable.Empty<StockTakeEntryDto>())
            {
                var (dimension, reportingUnit, unitsPerCountInBase) = DeriveFactors(entry);

                if (!HasAnyObservation(entry))
                {
                    var msg = $"[{entry.StockItemId}] {Safe(entry.Name)} — No observations captured (neither Count nor Weight).";
                    notes.Add(msg);
                    _logger.LogWarning("StockTake: {Message}", msg);
                    rejects.Add(RejectedStockTakeDto.IncompleteObservations(entry, sheetDate));
                    continue;
                }

                var hasWeight = HasWeightObservation(entry);

                if (dimension == MeasurementDimension.Volume && hasWeight && unitsPerCountInBase <= 1m)
                {
                    var msg = $"[{entry.StockItemId}] {Safe(entry.Name)} — Weight provided but item size is unknown (cannot convert weight to volume).";
                    notes.Add(msg);
                    _logger.LogWarning("StockTake: {Message}", msg);
                    rejects.Add(RejectedStockTakeDto.IncompleteObservations(entry, sheetDate));
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

                if (!WithinTolerance(observed, adjustedEstimate, tol))
                {
                    var msg = $"[{entry.StockItemId}] {Safe(entry.Name)} — Out of tolerance: Observed={observed:0.##} vs Estimate={adjustedEstimate:0.##} (±{tol:0.#}%).";
                    notes.Add(msg);
                    _logger.LogInformation("StockTake: {Message}", msg);
                    rejects.Add(RejectedStockTakeDto.OutOfTolerance(entry, sheetDate, observed, adjustedEstimate, tol));
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
                if (!ok)
                {
                    foreach (var kv in include)
                    {
                        var msg = $"[{kv.Key}] Persist failed in backend; item excluded.";
                        notes.Add(msg);
                        _logger.LogError("StockTake: {Message}", msg);
                        rejects.Add(RejectedStockTakeDto.BackendPersistFailed(kv.Key, sheetDate));
                    }
                    include.Clear();
                }
            }

            var header = $"StockTake {request.Date:yyyy-MM-dd} / {request.Division}: accepted={include.Count}, investigate={rejects.Count}.";
            var details = notes.Count == 0 ? "No issues detected." : string.Join("\n", notes.Select(n => $" - {n}"));
            var summary = $"{header}\n{details}";

            var completionTicket = new StockTakeCompletedTicketCommandDto(
                Date: request.Date,
                Division: request.Division,
                InvestigateItems: Array.Empty<StockTakeReportItemDto>(),
                AcceptedItems: Array.Empty<StockTakeReportItemDto>(),
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

        private static bool WithinTolerance(decimal observed, decimal estimate, decimal tolPercent)
        {
            var denom = Math.Max(Math.Abs(estimate), 0.000001m);
            var diffPct = (Math.Abs(observed - estimate) / denom) * 100m;
            return diffPct <= tolPercent;
        }

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
}
