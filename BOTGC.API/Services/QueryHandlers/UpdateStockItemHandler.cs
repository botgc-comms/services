using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class UpdateStockItemHandler(
    IOptions<AppSettings> settings,
    ILogger<UpdateStockItemHandler> logger,
    IDataProvider dataProvider,
    IReportParser<StockItemEditDialogDto> reportParser
) : QueryHandlerBase<UpdateStockItemCommand, bool>
{
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<UpdateStockItemHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    private readonly IReportParser<StockItemEditDialogDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

    public async override Task<bool> Handle(UpdateStockItemCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Updating stock item {StockId}.", request.StockId);  

            var url = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.UpdateStockItemUrl.Replace("{id}", request.StockId.ToString())}";  
            
            var stockItemData = await _dataProvider.GetData<StockItemEditDialogDto>(url, _reportParser);
            if (stockItemData != null && stockItemData.Any())
            {
                var stockItem = stockItemData[0];

                if (request.Name != null) stockItem.Name = request.Name;
                if (request.MinAlert != null) stockItem.MinAlert = request.MinAlert.Value;
                if (request.MaxAlert != null) stockItem.MaxAlert = request.MaxAlert.Value;

                try
                {
                    var data = BuildForm(stockItem);
                    var result = await _dataProvider.PostData(url + "&requestType=ajax&ajaxaction=saveItem&requestType=ajax&ajaxaction=saveItem", data);

                    return true;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update stock item {StockId}.", request.StockId);
                    return false;
                }
            }
            else
            {
                _logger.LogError("No stock item data found for StockId {StockId}.", request.StockId);
                return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve stock item data for StockId {StockId}.", request.StockId); 
            return false;
        }
    }

    private static Dictionary<string, string> BuildForm(StockItemEditDialogDto item)
    {
        var form = new Dictionary<string, string>
            {
                { "id", item.Id.ToString(CultureInfo.InvariantCulture) },
                { "name", item.Name ?? string.Empty },
                { "base_unit_id", item.BaseUnitId == null ? "" : item.BaseUnitId.Value.ToString(CultureInfo.InvariantCulture) },
                { "division_id", item.DivisionId == null ? "" : item.DivisionId.Value.ToString(CultureInfo.InvariantCulture) },
                { "min_alert", item.MinAlert.HasValue ? item.MinAlert.Value.ToString(CultureInfo.InvariantCulture) : string.Empty },
                { "max_alert", item.MaxAlert.HasValue ? item.MaxAlert.Value.ToString(CultureInfo.InvariantCulture) : string.Empty }
            };

        if (item.Active) form["active"] = "1";

        if (item.TradeUnits != null && item.TradeUnits.Count > 0)
        {
            for (var i = 0; i < item.TradeUnits.Count; i++)
            {
                var tu = item.TradeUnits[i];
                form[$"ids[{i}]"] = tu.Id.ToString(CultureInfo.InvariantCulture);
                form[$"names[{i}]"] = tu.Name ?? string.Empty;
                form[$"costs[{i}]"] = tu.Cost.ToString(CultureInfo.InvariantCulture);
                form[$"conversion_ratios[{i}]"] = tu.ConversionRatio.ToString(CultureInfo.InvariantCulture);
                form[$"precision_of_units[{i}]"] = tu.PrecisionOfUnit.ToString(CultureInfo.InvariantCulture);
            }
        }

        return form;
    }

}



