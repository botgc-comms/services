using BOTGC.API.Dto;
using BOTGC.API.Services.Queries;
using MediatR;

namespace BOTGC.API.Services.Behaviours;

public sealed class GetStockItemsAndTradeUnitsBehaviour(
        IMediator mediator,
        ILogger<GetStockItemsAndTradeUnitsBehaviour> logger
    ) : IPipelineBehavior<CreatePurchaseOrderFromDraftCommand, bool>
{
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILogger<GetStockItemsAndTradeUnitsBehaviour> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

    public async Task<bool> Handle(CreatePurchaseOrderFromDraftCommand request, RequestHandlerDelegate<bool> next, CancellationToken cancellationToken)
    {
        const int selectedStockRoomId = 1;

        var draft = request.Draft;
        var results = new PurchaseOrderItemDto[draft.Items.Count];

        using var gate = new SemaphoreSlim(5, 5);
        var tasks = draft.Items
            .Select((d, idx) => EnrichOneAsync(d, idx, results, selectedStockRoomId, gate, cancellationToken))
            .ToArray();

        try
        {
            await Task.WhenAll(tasks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enrich purchase order draft for reference {Reference}.", draft.OrderReference);
            return false;
        }

        var full = new PurchaseOrderDto
        {
            OrderReference = draft.OrderReference,
            Supplier = draft.Supplier,
            Items = results
        };

        return await _mediator.Send(new CreatePurchaseOrderCommand(full), cancellationToken);
    }

    private async Task EnrichOneAsync(
        PurchaseOrderDraftItemDto d,
        int index,
        PurchaseOrderItemDto[] sink,
        int selectedStockRoomId,
        SemaphoreSlim gate,
        CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            var units = await _mediator.Send(new GetStockItemUnitsQuery(d.StockItemId), ct);
            var chosen = units.FirstOrDefault();
            if (chosen == default)
            {
                throw new InvalidOperationException($"No units returned for StockItemId {d.StockItemId}.");
            }

            sink[index] = new PurchaseOrderItemDto
            {
                Id = string.Empty,
                TillStockItemId = d.StockItemId,
                StockItemId = d.StockItemId,
                TillStockItemUnitId = chosen.UnitId,
                UnitCost = chosen.Cost,
                Quantity = d.Quantity,
                Price = d.Price,
                SelectedStockRoomId = selectedStockRoomId
            };
        }
        finally
        {
            gate.Release();
        }
    }
}
