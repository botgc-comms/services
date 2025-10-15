using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class StockTakeCompletedHandler(IOptions<AppSettings> settings,
                                           IMediator mediator,
                                           ILogger<StockTakeCompletedHandler> logger) : QueryHandlerBase<StockTakeCompletedCommand, bool>
    {
        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
        private readonly ILogger<StockTakeCompletedHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        public async override Task<bool> Handle(StockTakeCompletedCommand request, CancellationToken cancellationToken)
        {
            var createTicketCommand = new CreateStockTakeMondayTicketsCommand(request);

            var result = await _mediator.Send(createTicketCommand, cancellationToken);

            return result;
        }
    }
}
