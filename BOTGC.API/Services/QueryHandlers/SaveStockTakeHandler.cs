using BOTGC.API;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using BOTGC.API.Services.QueryHandlers;
using MediatR;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.Options;
using System.Globalization;

namespace BOTGC.API.Services.QueryHandlers;

public sealed partial class SaveStockTakeHandler(IOptions<AppSettings> settings,
                            IMediator mediator,
                            ILogger<SaveStockTakeHandler> logger,
                            IDataProvider dataProvider) : QueryHandlerBase<SaveStockTakeCommand, int?>
{
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<SaveStockTakeHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

    public async override Task<int?> Handle(SaveStockTakeCommand request, CancellationToken cancellationToken)
    {
        try
        {
            var baseUrl = _settings.IG.BaseUrl?.TrimEnd('/') ?? throw new InvalidOperationException("Missing IG.BaseUrl in settings.");
            var url = $"{baseUrl}/{_settings.IG.Urls.SaveStockTakeUrl}";

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

            if (result != null)
            {
                var getStockTakesList = new GetStockTakesListQuery();
                var stockTakes = await _mediator.Send(getStockTakesList, cancellationToken);    

                if (stockTakes != null && stockTakes.Any())
                {
                    // Get the id of the most recent stock take
                    var latest = stockTakes.OrderByDescending(st => st.CreatedAt).FirstOrDefault();
                    if (latest != null)
                    {
                        _logger.LogInformation($"Successfully saved stock take with id {latest.Id} taken at {latest.CreatedAt}.");
                        return latest.Id;
                    }
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save stock take.");
            return null;
        }
    }
}
