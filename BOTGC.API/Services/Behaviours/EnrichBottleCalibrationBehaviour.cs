using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BOTGC.API.Dto;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Logging;

namespace BOTGC.API.Services.Behaviours;

public sealed class EnrichBottleCalibrationBehaviour
    : IPipelineBehavior<GetStockTakeSheetQuery, StockTakeSheetDto>
{
    private readonly IMediator _mediator;
    private readonly ILogger<EnrichBottleCalibrationBehaviour> _log;

    public EnrichBottleCalibrationBehaviour(IMediator mediator, ILogger<EnrichBottleCalibrationBehaviour> log)
    {
        _mediator = mediator;
        _log = log;
    }

    public async Task<StockTakeSheetDto> Handle(
        GetStockTakeSheetQuery request,
        RequestHandlerDelegate<StockTakeSheetDto> next,
        CancellationToken ct)
    {
        var sheet = await next();
        if (sheet is null || sheet.Entries.Count == 0) return sheet;

        foreach (var e in sheet.Entries)
        {
            if (!string.Equals(e.Unit, "BOTTLE", StringComparison.OrdinalIgnoreCase)) continue;

            var res = await _mediator.Send(new BottleCalibrationLookupQuery(e.StockItemId, e.Name, e.Division), ct);
            if (res.Entity is null) continue;

            e.Calibration = new BottleCalibrationDto(
                res.Entity.NominalVolumeMl,
                res.Entity.EmptyWeightGrams,
                res.Entity.FullWeightGrams,
                res.Confidence,
                res.Strategy
            );
            e.CalibrationHighConfidence = res.IsHighConfidence;
        }

        _log.LogDebug("Enriched sheet with bottle calibration for {Count} entries.", sheet.Entries.Count);
        return sheet;
    }
}
