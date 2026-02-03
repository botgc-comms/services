using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public class GetParentsQueryHandler(IOptions<AppSettings> settings,
                                    ILogger<GetParentsQueryHandler> logger,
                                    IDataProvider dataProvider,
                                    IReportParser<ParentChildDto> reportParser) : QueryHandlerBase<GetParentsQuery, List<ParentChildDto>>
{
    private const string __CACHE_KEY = "Parents";

    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly ILogger<GetParentsQueryHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
    private readonly IReportParser<ParentChildDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

    public async override Task<List<ParentChildDto>> Handle(GetParentsQuery request, CancellationToken cancellationToken)
    {
        var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.ParentChildUrl}";
        var parentChildRelationships = await _dataProvider.GetData<ParentChildDto>(reportUrl, _reportParser, __CACHE_KEY, TimeSpan.FromMinutes(_settings.Cache.ShortTerm_TTL_mins));

        _logger.LogInformation("Found {Count} parents with children.", parentChildRelationships.Count);

        return parentChildRelationships;
    }
}
