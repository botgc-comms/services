using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public class GetTillProductsHandler(IOptions<AppSettings> settings,
                                    IMediator mediator,
                                    ILogger<GetTillProductsHandler> logger,
                                    IReportParser<TillProductLookupDto> reportParser,
                                    IDataProvider dataProvider) : QueryHandlerBase<GetTillProductsQuery, List<TillProductInformationDto>?>
{
    private const string CacheKey = "Till_Products";
    private const int DegreeOfParallelism = 5;

    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILogger<GetTillProductsHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IReportParser<TillProductLookupDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));
    private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));

    public async override Task<List<TillProductInformationDto>?> Handle(GetTillProductsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.TillProductsReportUrl}";
            var ttl = TimeSpan.FromMinutes(_settings.Cache.MediumTerm_TTL_mins);
            var tillProducts = await _dataProvider.GetData<TillProductLookupDto>(reportUrl, _reportParser, CacheKey, ttl);

            if (tillProducts == null || tillProducts.Count == 0)
            {
                _logger.LogWarning("No till products found in the report.");
                return new List<TillProductInformationDto>();
            }

            _logger.LogInformation("Successfully retrieved {Count} products.", tillProducts.Count);

            using var gate = new SemaphoreSlim(DegreeOfParallelism, DegreeOfParallelism);
            var tasks = tillProducts.Select(p => EnrichOneAsync(p, gate, cancellationToken)).ToArray();

            try
            {
                var results = await Task.WhenAll(tasks);
                var retVal = results.Where(r => r != null).Select(r => r!).ToList();
                return retVal;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to compose till product information.");
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve till products.");
            return null;
        }
    }

    private async Task<TillProductInformationDto?> EnrichOneAsync(TillProductLookupDto item, SemaphoreSlim gate, CancellationToken ct)
    {
        await gate.WaitAsync(ct);
        try
        {
            return await _mediator.Send(new GetTillProductInformationQuery(item.ProductId), ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get detailed info for till product {ProductId}.", item.ProductId);
            return null;
        }
        finally
        {
            gate.Release();
        }
    }
}


