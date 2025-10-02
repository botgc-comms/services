using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public partial class GetWasteSheetHandler(IOptions<AppSettings> settings,
                                  IMediator mediator,
                                  ILogger<GetWasteSheetHandler> logger,
                                  IServiceScopeFactory serviceScopeFactory)
    : QueryHandlerBase<GetWasteSheetQuery, WasteSheetDto?>
{
    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILogger<GetWasteSheetHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IServiceScopeFactory _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

    public async override Task<WasteSheetDto?> Handle(GetWasteSheetQuery request, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

        var CacheKey = (DateTime d) => $"WasteSheet:{d:yyyyMMdd}";

        var date = request.Date.Date;
        var key = CacheKey(date);

        var cached = await cache.GetAsync<WasteSheetCacheModel>(key).ConfigureAwait(false);
        if (cached is null)
        {
            _logger.LogInformation("No cached waste sheet for {Date}. Returning empty open sheet.", date.ToString("yyyy-MM-dd"));
            return new WasteSheetDto(date, "Open", new List<WasteEntryDto>());
        }

        var list = cached.Entries.Values
            .OrderBy(e => e.CreatedAtUtc)
            .ToList();

        _logger.LogInformation("Returning waste sheet for {Date} with {Count} entries.", date.ToString("yyyy-MM-dd"), list.Count);
        return new WasteSheetDto(date, cached.Status, list);
    }
}