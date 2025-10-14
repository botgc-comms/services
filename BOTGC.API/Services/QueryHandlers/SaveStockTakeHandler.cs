using BOTGC.API;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using BOTGC.API.Services.QueryHandlers;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class SaveStockTakeHandler : QueryHandlerBase<SaveStockTakeCommand, bool>
{
    private readonly AppSettings _settings;
    private readonly ILogger<SaveStockTakeHandler> _logger;
    private readonly IDataProvider _dataProvider;

    public SaveStockTakeHandler(IOptions<AppSettings> settings,
                                ILogger<SaveStockTakeHandler> logger,
                                IDataProvider dataProvider)
    {
        _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    }

    public async override Task<bool> Handle(SaveStockTakeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = _settings.IG.BaseUrl?.TrimEnd('/') ?? throw new InvalidOperationException("Missing IG.BaseUrl in settings.");
            var url = $"{baseUrl}/tillstockcontrol.php?tab=take&section=new&requestType=ajax&ajaxaction=saveStockTake";

            var data = new Dictionary<string, string>
            {
                { "datetime", request.TakenAtLocal.ToString("yyyy-MM-dd H:mm:ss", CultureInfo.InvariantCulture) },
                { "confirm", "1" }
            };

            foreach (var kv in request.Quantities)
            {
                data[$"tillStockItems[{kv.Key}]"] = kv.Value.ToString(CultureInfo.InvariantCulture);
            }

            if (request.Reasons != null)
            {
                foreach (var rv in request.Reasons)
                {
                    data[$"tillStockItemReasons[{rv.Key}]"] = rv.Value ?? string.Empty;
                }
            }

            var result = await _dataProvider.PostData(url, data);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save stock take.");
            return false;
        }
    }
}


