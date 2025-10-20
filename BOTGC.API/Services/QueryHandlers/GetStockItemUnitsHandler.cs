using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class GetStockItemUnitsHandler(
        IOptions<AppSettings> settings,
        ILogger<GetStockItemUnitsHandler> logger,
        IDataProvider dataProvider
    ) : QueryHandlerBase<GetStockItemUnitsQuery, IReadOnlyList<StockItemUnitInfoDto>>
{
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<GetStockItemUnitsHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));

    public async override Task<IReadOnlyList<StockItemUnitInfoDto>> Handle(GetStockItemUnitsQuery request, CancellationToken cancellationToken)
    {
        var url = $"{_settings.IG.BaseUrl}/tillstockcontrol.php?tab=purchase_orders&requestType=ajax&ajaxaction=getStockItemUnits";
        var form = new Dictionary<string, string> { { "stock_item_id", request.StockItemId.ToString() } };
        var headers = new Dictionary<string, string>
            {
                { "X-Requested-With", "XMLHttpRequest" },
                { "Referer", $"{_settings.IG.BaseUrl}/tillstockcontrol.php?tab=purchase_orders&page=1&received=1" }
            };

        var json = await _dataProvider.PostData(url, form);
        if (string.IsNullOrWhiteSpace(json))
        {
            _logger.LogError("Empty response from getStockItemUnits for StockItemId {Id}.", request.StockItemId);
            return Array.Empty<StockItemUnitInfoDto>();
        }

        using var doc = JsonDocument.Parse(json);
        var list = new List<StockItemUnitInfoDto>();
        foreach (var el in doc.RootElement.EnumerateArray())
        {
            var id = el.GetProperty("id").GetString();
            var name = el.GetProperty("name").GetString();
            var cost = el.GetProperty("cost").GetString();
            if (int.TryParse(id, out var unitId) && decimal.TryParse(cost, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var unitCost))
            {
                list.Add(new StockItemUnitInfoDto(unitId, name ?? string.Empty, unitCost));
            }
        }

        return list;
    }
}