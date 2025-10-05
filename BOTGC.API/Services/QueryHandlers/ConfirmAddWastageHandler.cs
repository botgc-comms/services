using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class ConfirmAddWastageHandler(IOptions<AppSettings> settings,
                                          ILogger<ConfirmAddWastageHandler> logger,
                                          IDataProvider dataProvider) : QueryHandlerBase<ConfirmAddWastageCommand, bool>
        {
            private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            private readonly ILogger<ConfirmAddWastageHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));

            public async override Task<bool> Handle(ConfirmAddWastageCommand request, CancellationToken cancellationToken)
            {
                try
                {
                    var url = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.ConfirmAddWastageUrl}";

                    var data = new Dictionary<string, string>
                {
                    { "id", request.ProductId.ToString() },
                    { "stockRoomId", request.StockRoomId.ToString() },
                    { "qty", request.Quantity.ToString() },
                    { "reason", request.Reason }
                };

                    await _dataProvider.PostData(url, data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to add wastage for product {ProductId} (stockRoomId={StockRoomId}, qty={Quantity}).", request.ProductId, request.StockRoomId, request.Quantity);
                    return false;
                }

                return true;
            }
        }
    }
