using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using BOTGC.API.Services.QueryHandlers;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public class GetStockItemsAndTradeUnitsHandler(IOptions<AppSettings> settings,
                                               IMediator mediator,
                                               ILogger<GetStockItemsAndTradeUnitsHandler> logger,
                                               IDataProvider dataProvider) : QueryHandlerBase<GetStockItemsAndTradeUnitsQuery, List<StockItemAndTradeUnitDto>>
{
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILogger<GetStockItemsAndTradeUnitsHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));

    public async override Task<List<StockItemAndTradeUnitDto>> Handle(GetStockItemsAndTradeUnitsQuery request, CancellationToken cancellationToken)
    {
        var stockItems = await _mediator.Send(new GetStockLevelsQuery(), cancellationToken);

        var retVal = stockItems.Select(si => new StockItemAndTradeUnitDto
        {
            Id = si.Id,
            Name = si.Name,
            Unit = si.Unit,
            Division = si.Division,
            IsActive = si.IsActive,
            TradeUnits = new List<StockItemUnitInfoDto>()
        }).ToList();

        using var gate = new SemaphoreSlim(5, 5);
        var tasks = retVal.Select(d => EnrichOneAsync(d, gate, cancellationToken)).ToArray();

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to compose stock items with trade units.");
            throw;
        }

        return retVal;
    }

    private async Task EnrichOneAsync(StockItemAndTradeUnitDto item, SemaphoreSlim gate, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            var units = await _mediator.Send(new GetStockItemUnitsQuery(item.Id), ct);
            item.TradeUnits = units != null ? units.ToList() : new List<StockItemUnitInfoDto>();

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get trade units for stock item {StockItemId}.", item.Id);
        }
        finally
        {
            gate.Release();
        }
    }
}

