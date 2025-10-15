using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public partial class GetStockTakeSheetHandler
{
    public class GetHandler(IOptions<AppSettings> settings,
                            ILogger<GetHandler> logger,
                            IMediator mediator)
        : QueryHandlerBase<GetStockTakeSheetQuery, StockTakeSheetDto>
    {
        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<GetHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));

        public async override Task<StockTakeSheetDto> Handle(GetStockTakeSheetQuery request, CancellationToken cancellationToken)
        {
            var date = request.Date.Date;
            var division = request.Division?.Trim() ?? string.Empty;

            var suggestions = await _mediator.Send(new GetStockTakeProductsQuery(), cancellationToken);
            var div = suggestions.FirstOrDefault(x => string.Equals(x.Division, division, StringComparison.OrdinalIgnoreCase));

            var entries = new List<StockTakeEntryDto>();
            if (div is not null)
            {
                foreach (var p in div.Products)
                {
                    entries.Add(new StockTakeEntryDto(
                        StockItemId: p.StockItemId,
                        Name: p.Name,
                        Division: p.Division,
                        Unit: p.Unit,
                        OperatorId: Guid.Empty,
                        OperatorName: string.Empty,
                        At: default,
                        Observations: new List<StockTakeObservationDto>(),
                        EstimatedQuantityAtCapture: p.CurrentQuantity ?? 0m
                    ));
                }
            }

            return new StockTakeSheetDto(date, division, "Open", entries.OrderBy(e => e.Name).ToList());
        }
    }
}
