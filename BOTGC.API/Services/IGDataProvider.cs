using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services
{
    public class IGDataProvider : IDataProvider
    {
        private const string RawSuffix = "__raw";

        private readonly AppSettings _settings;
        private readonly ILogger<IGDataProvider> _logger;
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly IGSessionService _igSessionManagementService;

        public IGDataProvider(IOptions<AppSettings> settings,
                              ILogger<IGDataProvider> logger,
                              IGSessionService igSessionManagementService,
                              IServiceScopeFactory serviceScopeFactory)
        {
            _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));
            _igSessionManagementService = igSessionManagementService ?? throw new ArgumentNullException(nameof(igSessionManagementService));
        }

        public async Task<string?> PostData(string reportUrl,
                                            Dictionary<string, string> data)
        {
            await _igSessionManagementService.WaitForLoginAsync();

            _logger.LogInformation("Sending data to {Url}", reportUrl);
            var response = await _igSessionManagementService.PostPageContentRaw(reportUrl, data);

            return response;
        }

        public async Task<List<T>> PostData<T>(string reportUrl,
                                               Dictionary<string, string> data,
                                               IReportParser<T> parser,
                                               string? cacheKey = null,
                                               TimeSpan? cacheTTL = null,
                                               Func<T, List<HateoasLink>>? linkBuilder = null) where T : HateoasResource, new()
        {
            ICacheService? cacheService = null;

            if (!string.IsNullOrEmpty(cacheKey))
            {
                using var scope = _serviceScopeFactory.CreateScope();
                cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

                var cachedResults = await cacheService!.GetAsync<List<T>>(cacheKey).ConfigureAwait(false);
                if (cachedResults != null && cachedResults.Any())
                {
                    _logger.LogInformation("Retrieving results from cache for {ReportType}...", typeof(T).Name);
                    return cachedResults;
                }
            }

            _logger.LogInformation("Starting report retrieval for {ReportType}...", typeof(T).Name);

            await _igSessionManagementService.WaitForLoginAsync();

            _logger.LogInformation("Fetching {ReportType} report from {Url}", typeof(T).Name, reportUrl);
            var doc = await _igSessionManagementService.PostPageContent(reportUrl, data);

            if (doc != null)
            {
                var items = await parser.ParseReport(doc);

                if (linkBuilder != null)
                {
                    foreach (var item in items)
                    {
                        item.Links = linkBuilder(item);
                    }
                }

                if (!string.IsNullOrEmpty(cacheKey))
                {
                    var ttl = cacheTTL ?? TimeSpan.FromMinutes(_settings.Cache.Default_TTL_Mins);
                    var rawTtl = TimeSpan.FromHours(1);

                    await cacheService!.SetAsync(cacheKey!, items, ttl).ConfigureAwait(false);

                    var rawKey = cacheKey + RawSuffix;
                    var rawHtml = doc.DocumentNode.OuterHtml;
                    await cacheService.SetAsync(rawKey, rawHtml, rawTtl).ConfigureAwait(false);
                }

                return items;
            }
            else
            {
                _logger.LogError("Failed to retrieve data from {Url} for {ReportType}", reportUrl, typeof(T).Name);
                return null!;
            }
        }

        public async Task<List<T>> GetData<T>(
            string reportUrl,
            IReportParser<T> parser,
            string? cacheKey = null,
            TimeSpan? cacheTTL = null,
            Func<T, List<HateoasLink>>? linkBuilder = null
        ) where T : HateoasResource, new()
        {
            return await ExecuteGet(reportUrl, async doc => await parser.ParseReport(doc), cacheKey, cacheTTL, linkBuilder);
        }

        public async Task<List<T>> GetData<T, TMetadata>(
            string reportUrl,
            IReportParserWithMetadata<T, TMetadata> parser,
            TMetadata metadata,
            string? cacheKey = null,
            TimeSpan? cacheTTL = null,
            Func<T, List<HateoasLink>>? linkBuilder = null
        ) where T : HateoasResource, new()
        {
            return await ExecuteGet(reportUrl, async doc => await parser.ParseReport(doc, metadata), cacheKey, cacheTTL, linkBuilder);
        }

        private async Task<List<T>> ExecuteGet<T>(
            string reportUrl,
            Func<HtmlDocument, Task<List<T>>> parse,
            string? cacheKey,
            TimeSpan? cacheTTL,
            Func<T, List<HateoasLink>>? linkBuilder
        ) where T : HateoasResource, new()
        {
            ICacheService? cacheService = null;

            if (!string.IsNullOrEmpty(cacheKey))
            {
                cacheTTL = cacheTTL ?? TimeSpan.FromMinutes(_settings.Cache.Default_TTL_Mins);

                using var scope = _serviceScopeFactory.CreateScope();
                cacheService = scope.ServiceProvider.GetRequiredService<ICacheService>();

                var cachedResults = await cacheService!.GetAsync<List<T>>(cacheKey).ConfigureAwait(false);
                if (cachedResults != null && cachedResults.Any())
                {
                    _logger.LogInformation("Retrieving results from cache for {ReportType}...", typeof(T).Name);
                    return cachedResults;
                }
            }

            _logger.LogInformation("Starting report retrieval for {ReportType}...", typeof(T).Name);

            await _igSessionManagementService.WaitForLoginAsync();

            _logger.LogInformation("Fetching {ReportType} report from {Url}", typeof(T).Name, reportUrl);
            var doc = await _igSessionManagementService.GetPageContent(reportUrl);

            var items = await parse(doc);

            if (linkBuilder != null)
            {
                foreach (var item in items)
                {
                    item.Links = linkBuilder(item);
                }
            }

            if (!string.IsNullOrEmpty(cacheKey) && cacheTTL != null && items.Any())
            {
                var rawTtl = TimeSpan.FromHours(1);

                await cacheService!.SetAsync(cacheKey!, items, cacheTTL.Value).ConfigureAwait(false);

                var rawKey = cacheKey + RawSuffix;
                var rawHtml = doc.DocumentNode.OuterHtml;
                await cacheService.SetAsync(rawKey, rawHtml, rawTtl).ConfigureAwait(false);
            }

            return items;
        }
    }
}
