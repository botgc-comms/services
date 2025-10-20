using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class CreatePurchaseOrderHandler(
    IOptions<AppSettings> settings,
    ILogger<CreatePurchaseOrderHandler> logger,
    IDataProvider dataProvider
) : QueryHandlerBase<CreatePurchaseOrderCommand, bool>
{
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<CreatePurchaseOrderHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));

    public async override Task<bool> Handle(CreatePurchaseOrderCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.CreatePurchaseOrderUrl.Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"))}";
            var data = new Dictionary<string, string>
                {
                    { "orderReference", request.Order.OrderReference },
                    { "supplier", request.Order.Supplier.ToString() }
                };

            for (var i = 0; i < request.Order.Items.Count; i++)
            {
                var it = request.Order.Items[i];
                data[$"ids[{i}]"] = it.Id ?? string.Empty;
                data[$"tillStockItems[{i}]"] = it.TillStockItemId.ToString();
                data[$"stockItems[{i}]"] = it.StockItemId.ToString();
                data[$"tillStockItemUnitsHidden[{i}]"] = it.TillStockItemUnitId.ToString();
                data[$"unitcosts[{i}]"] = it.UnitCost.ToString("0.00");
                data[$"quantities[{i}]"] = it.Quantity.ToString("0.###");
                data[$"prices[{i}]"] = it.Price.ToString("0.00");
                data[$"selectedStockRoom[{i}]"] = it.SelectedStockRoomId.ToString();
            }

            await _dataProvider.PostData(url, data);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to confirm delivery for order {OrderReference}.", request.Order.OrderReference);
            return false;
        }
    }
}



