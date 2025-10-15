using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public class GetStockTakesHandler(IOptions<AppSettings> settings,
                                    IMediator mediator,
                                    ILogger<GetStockTakesHandler> logger,
                                    IDataProvider dataProvider,
                                    IReportParser<StockTakeReportEntryDto> reportParser)
    : QueryHandlerBase<GetStockTakesQuery, List<StockTakeSummaryDto>>
{
    private const string __CACHE_KEY = "Stock_Takes_{fromDate}_{toDate}";

    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<GetStockTakesHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly IReportParser<StockTakeReportEntryDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

    public async override Task<List<StockTakeSummaryDto>> Handle(GetStockTakesQuery request, CancellationToken cancellationToken)
    {
        var tzId = _settings.Waste?.TimeZone ?? "Europe/London";
        var tz = TimeZoneInfo.FindSystemTimeZoneById(tzId);
        var todayLocal = TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, tz).Date;

        var from = (request.FromDate ?? todayLocal.AddYears(-1));
        var to = request.ToDate ?? todayLocal;

        var cacheKey = __CACHE_KEY
            .Replace("{fromDate}", from.ToString("yyyy-MM-dd"))
            .Replace("{toDate}", to.ToString("yyyy-MM-dd"));

        var form = new Dictionary<string, string>
        {
            { "rangetype", "CU" },
            { "datefrom", from.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) },
            { "timefrom", "00:00" },
            { "dateto", to.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) },
            { "timeto", "23:59" },
            { "till_config_id", request.TillConfigId.ToString(CultureInfo.InvariantCulture) },
            { "zread", request.ZRead.ToString(CultureInfo.InvariantCulture) },
            { "stock_item", request.StockItemId.ToString(CultureInfo.InvariantCulture) },
            { "stock_action", "stock take" }
        };

        var ttl = TimeSpan.FromMinutes(_settings.Cache.MediumTerm_TTL_mins);
        var url = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.GetStockTakesReportUrl}";

        var flatRows = await _dataProvider.PostData<StockTakeReportEntryDto>(
            url,
            form,
            _reportParser,
            cacheKey,
            ttl
        ) ?? new List<StockTakeReportEntryDto>();

        // Get all active stock items
        var stockItemsQuery = new GetStockLevelsQuery();
        var products = await _mediator.Send(stockItemsQuery, cancellationToken) ?? new List<StockItemDto>();

        var activeProducts = products.Where(p => p.IsActive.GetValueOrDefault(true));

        var grouped = new List<StockTakeSummaryDto>();

        foreach (var product in activeProducts)
        {
            // Find all stock take entries for this product
            var entries = flatRows.Where(r => r.Name.ToLower() == product.Name.ToLower()).OrderBy(r => r.Timestamp ?? DateTimeOffset.MinValue).ToList();

            var snapshots = entries
                .Where(r => r.Timestamp.HasValue)
                .Select(r => new StockTakeSnapshotDto
                {
                    Timestamp = r.Timestamp!.Value,
                    Before = r.PreviousQuantity,
                    After = r.NewQuantity,
                    Adjustment = r.Difference
                })
                .ToList();

            var last = entries.LastOrDefault();

            var lastTs = last?.Timestamp;
            int? daysSince = null;
            if (lastTs.HasValue)
            {
                var lastLocalDate = TimeZoneInfo.ConvertTime(lastTs.Value, tz).Date;
                daysSince = (int)(todayLocal - lastLocalDate).TotalDays;
            }

            var currentQty = product.Quantity
                                ?? last?.CurrentQuantity
                                ?? last?.NewQuantity;

            grouped.Add(new StockTakeSummaryDto
            {
                StockItemId = product.Id,
                Name = product.Name,
                Unit = product.Unit,
                Division = product.Division,
                CurrentQuantity = currentQty,
                LastStockTake = lastTs,
                DaysSinceLastStockTake = daysSince,
                StockTakes = snapshots
            });
        }

        if (grouped.Count > 0)
        {
            _logger.LogInformation("Built grouped stock-take summary for {Count} items from {From} to {To}.", grouped.Count, from, to);
        }
        else
        {
            _logger.LogInformation("No stock-take rows found for {From} to {To}.", from, to);
        }

        return grouped;
    }
}


