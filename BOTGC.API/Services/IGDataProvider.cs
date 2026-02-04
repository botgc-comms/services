using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using HtmlAgilityPack;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace BOTGC.API.Services
{
    public class IGDataProvider(
        IOptions<AppSettings> settings,
        ILogger<IGDataProvider> logger,
        IGSessionService igSessionManagementService,
        ICacheService cacheService,
        IQueryCacheOptionsAccessor cacheOptions,
        IDistributedLockManager lockManager,
        IIGScrapeTelemetry telemetry) : IDataProvider
    {
        private const string __RAWSUFFIX = "__raw";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<IGDataProvider> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly ICacheService _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        private readonly IGSessionService _igSessionManagementService = igSessionManagementService ?? throw new ArgumentNullException(nameof(igSessionManagementService));
        private readonly IQueryCacheOptionsAccessor _cacheOptions = cacheOptions ?? throw new ArgumentNullException(nameof(cacheOptions));
        private readonly IIGScrapeTelemetry _telemetry = telemetry ?? throw new ArgumentNullException(nameof(telemetry));

        private readonly IDistributedLockManager _lockManager = lockManager ?? throw new ArgumentNullException(nameof(lockManager));

        public async Task<string?> PostData(string reportUrl,
                                            Dictionary<string, string> data)
        {
            await _igSessionManagementService.WaitForLoginAsync();
            _logger.LogInformation("Sending data to {Url}", reportUrl);
            var response = await _igSessionManagementService.PostPageContentRaw(reportUrl, data);
            return response;
        }

        public async Task<List<T>> PostData<T>(
    string reportUrl,
    Dictionary<string, string> data,
    IReportParser<T> parser,
    string? cacheKey = null,
    TimeSpan? cacheTTL = null,
    Func<T, List<HateoasLink>>? linkBuilder = null
) where T : HateoasResource, new()
        {
            var dtoType = typeof(T).Name;
            using var activity = _telemetry.Start(dtoType, reportUrl, "POST", cacheKey != null, _settings.UseCachedHtml);

            var cache = _cacheOptions.Current;

            ICacheService cacheService = _cacheService;
            HtmlDocument? doc = null;

            if (!string.IsNullOrEmpty(cacheKey))
            {
                if (!cache.NoCache)
                {
                    var cachedResults = await cacheService.GetAsync<List<T>>(cacheKey).ConfigureAwait(false);
                    if (cachedResults != null && cachedResults.Any())
                    {
                        _telemetry.TrackCacheHit(dtoType, reportUrl, "Results", cachedResults.Count);
                        _logger.LogInformation("Retrieving results from cache for {ReportType}...", typeof(T).Name);
                        return cachedResults;
                    }

                    if (_settings.UseCachedHtml)
                    {
                        var rawKey = cacheKey + __RAWSUFFIX;
                        var rawHtml = await cacheService.GetAsync<string>(rawKey, true).ConfigureAwait(false);
                        if (!string.IsNullOrWhiteSpace(rawHtml))
                        {
                            try
                            {
                                _logger.LogInformation("Using cached raw HTML for {ReportType}...", typeof(T).Name);
                                doc = BuildHtmlDocumentFromRaw(rawHtml);
                                _telemetry.TrackCacheHit(dtoType, reportUrl, "RawHtml", null);
                                _logger.LogInformation("Successfully loaded cached raw HTML for {ReportType}...", typeof(T).Name);
                            }
                            catch (Exception ex)
                            {
                                _telemetry.TrackFailure(dtoType, reportUrl, "LoadCachedHtml", ex.GetType().Name);
                                _logger.LogError(ex, "Failed to load cached HTML for {ReportType}...", typeof(T).Name);
                                doc = null;
                            }
                        }
                    }

                    if (doc == null)
                    {
                        var lockResource = BuildReportLockResource(cacheKey);
                        await using var distLock = await _lockManager.AcquireLockAsync(lockResource, cancellationToken: CancellationToken.None);

                        if (distLock.IsAcquired)
                        {
                            var cachedResultsAfterLock = await cacheService.GetAsync<List<T>>(cacheKey).ConfigureAwait(false);
                            if (cachedResultsAfterLock != null && cachedResultsAfterLock.Any())
                            {
                                _telemetry.TrackCacheHit(dtoType, reportUrl, "Results", cachedResultsAfterLock.Count);
                                _logger.LogInformation("Retrieving results from cache for {ReportType} after lock...", typeof(T).Name);
                                return cachedResultsAfterLock;
                            }

                            if (_settings.UseCachedHtml)
                            {
                                var rawKey = cacheKey + __RAWSUFFIX;
                                var rawHtml = await cacheService.GetAsync<string>(rawKey, true).ConfigureAwait(false);
                                if (!string.IsNullOrWhiteSpace(rawHtml))
                                {
                                    try
                                    {
                                        _logger.LogInformation("Using cached raw HTML for {ReportType} after lock...", typeof(T).Name);
                                        doc = BuildHtmlDocumentFromRaw(rawHtml);
                                        _telemetry.TrackCacheHit(dtoType, reportUrl, "RawHtml", null);
                                        _logger.LogInformation("Successfully loaded cached raw HTML for {ReportType} after lock...", typeof(T).Name);
                                    }
                                    catch (Exception ex)
                                    {
                                        _telemetry.TrackFailure(dtoType, reportUrl, "LoadCachedHtmlAfterLock", ex.GetType().Name);
                                        _logger.LogError(ex, "Failed to load cached HTML for {ReportType} after lock...", typeof(T).Name);
                                        doc = null;
                                    }
                                }
                            }

                            _logger.LogInformation("Starting report retrieval for {ReportType}...", typeof(T).Name);

                            await _igSessionManagementService.WaitForLoginAsync();

                            if (doc == null)
                            {
                                _logger.LogInformation("Fetching {ReportType} report from {Url}", typeof(T).Name, reportUrl);

                                var swFetch = System.Diagnostics.Stopwatch.StartNew();
                                doc = await _igSessionManagementService.PostPageContent(reportUrl, data);
                                swFetch.Stop();

                                var bytes = IGDataProviderTelemetry.GetUtf8ByteCount(doc?.DocumentNode?.OuterHtml);
                                _telemetry.TrackFetch(dtoType, reportUrl, "POST", bytes, swFetch.ElapsedMilliseconds, doc != null);
                            }

                            List<T> itemsLocked;
                            try
                            {
                                var swParse = System.Diagnostics.Stopwatch.StartNew();
                                itemsLocked = await parser.ParseReport(doc);
                                swParse.Stop();

                                _telemetry.TrackParse(dtoType, reportUrl, itemsLocked?.Count ?? 0, swParse.ElapsedMilliseconds, itemsLocked != null);
                            }
                            catch (Exception ex)
                            {
                                _telemetry.TrackFailure(dtoType, reportUrl, "ParseReport", ex.GetType().Name);
                                throw;
                            }

                            if (linkBuilder != null)
                            {
                                foreach (var item in itemsLocked)
                                {
                                    item.Links = linkBuilder(item);
                                }
                            }

                            if (!string.IsNullOrEmpty(cacheKey) && cache.WriteThrough)
                            {
                                var ttl = cacheTTL ?? TimeSpan.FromMinutes(_settings.Cache.Default_TTL_Mins);
                                var rawTtl = TimeSpan.FromMinutes(15);

                                await cacheService.SetAsync(cacheKey, itemsLocked, ttl).ConfigureAwait(false);

                                if (_settings.UseCachedHtml)
                                {
                                    var rawKey = cacheKey + __RAWSUFFIX;
                                    var rawHtml = doc.DocumentNode.OuterHtml;
                                    await cacheService.SetAsync(rawKey, rawHtml, rawTtl).ConfigureAwait(false);
                                }
                            }

                            return itemsLocked;
                        }
                    }
                }
            }

            _logger.LogInformation("Starting report retrieval for {ReportType}...", typeof(T).Name);

            await _igSessionManagementService.WaitForLoginAsync();

            if (doc == null)
            {
                _logger.LogInformation("Fetching {ReportType} report from {Url}", typeof(T).Name, reportUrl);

                var swFetch = System.Diagnostics.Stopwatch.StartNew();
                doc = await _igSessionManagementService.PostPageContent(reportUrl, data);
                swFetch.Stop();

                var bytes = IGDataProviderTelemetry.GetUtf8ByteCount(doc?.DocumentNode?.OuterHtml);
                _telemetry.TrackFetch(dtoType, reportUrl, "POST", bytes, swFetch.ElapsedMilliseconds, doc != null);
            }

            List<T> items;
            try
            {
                var swParse = System.Diagnostics.Stopwatch.StartNew();
                items = await parser.ParseReport(doc);
                swParse.Stop();

                _telemetry.TrackParse(dtoType, reportUrl, items?.Count ?? 0, swParse.ElapsedMilliseconds, items != null);
            }
            catch (Exception ex)
            {
                _telemetry.TrackFailure(dtoType, reportUrl, "ParseReport", ex.GetType().Name);
                throw;
            }

            if (linkBuilder != null)
            {
                foreach (var item in items)
                {
                    item.Links = linkBuilder(item);
                }
            }

            if (!string.IsNullOrEmpty(cacheKey) && cache.WriteThrough)
            {
                var ttl = cacheTTL ?? TimeSpan.FromMinutes(_settings.Cache.Default_TTL_Mins);
                var rawTtl = TimeSpan.FromMinutes(15);

                await cacheService.SetAsync(cacheKey, items, ttl).ConfigureAwait(false);

                if (_settings.UseCachedHtml)
                {
                    var rawKey = cacheKey + __RAWSUFFIX;
                    var rawHtml = doc.DocumentNode.OuterHtml;
                    await cacheService.SetAsync(rawKey, rawHtml, rawTtl).ConfigureAwait(false);
                }
            }

            return items;
        }

        public async Task<List<T>> GetData<T>(
            string reportUrl,
            IReportParser<T> parser,
            string? cacheKey = null,
            TimeSpan? cacheTTL = null,
            Func<T, List<HateoasLink>>? linkBuilder = null
        ) where T : HateoasResource, new()
        {
            return await ExecuteGet(reportUrl, async doc => await parser.ParseReport(doc), cacheKey, cacheTTL, false, linkBuilder);
        }

        public async Task<List<T>> GetData<T>(
            string reportUrl,
            IReportParser<T> parser,
            string? cacheKey,
            TimeSpan? cacheTTL,
            bool noCache,
            Func<T, List<HateoasLink>>? linkBuilder = null
        ) where T : HateoasResource, new()
        {
            return await ExecuteGet(reportUrl, async doc => await parser.ParseReport(doc), cacheKey, cacheTTL, noCache, linkBuilder);
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
            return await ExecuteGet(reportUrl, async doc => await parser.ParseReport(doc, metadata), cacheKey, cacheTTL, false, linkBuilder);
        }

        private async Task<List<T>> ExecuteGet<T>(
            string reportUrl,
            Func<HtmlDocument, Task<List<T>>> parse,
            string? cacheKey,
            TimeSpan? cacheTTL,
            bool noCache,
            Func<T, List<HateoasLink>>? linkBuilder
        ) where T : HateoasResource, new()
        {
            var dtoType = typeof(T).Name;
            using var activity = _telemetry.Start(dtoType, reportUrl, "GET", cacheKey != null, _settings.UseCachedHtml);

            var cache = _cacheOptions.Current;
            var effectiveNoCache = noCache || cache.NoCache;

            _logger.LogInformation(
                "Cache policy for {ReportType} (key={CacheKey}): NoCache={NoCache}, WriteThrough={WriteThrough}, EffectiveNoCache={EffectiveNoCache}",
                typeof(T).Name,
                cacheKey,
                cache.NoCache,
                cache.WriteThrough,
                effectiveNoCache);

            HtmlDocument? doc = null;

            if (!string.IsNullOrEmpty(cacheKey))
            {
                cacheTTL ??= TimeSpan.FromMinutes(_settings.Cache.Default_TTL_Mins);

                if (!effectiveNoCache)
                {
                    var cachedResults = await _cacheService.GetAsync<List<T>>(cacheKey).ConfigureAwait(false);
                    if (cachedResults != null && cachedResults.Any())
                    {
                        _telemetry.TrackCacheHit(dtoType, reportUrl, "Results", cachedResults.Count);
                        _logger.LogInformation("Retrieving results from cache for {ReportType}...", typeof(T).Name);
                        return cachedResults;
                    }

                    if (_settings.UseCachedHtml)
                    {
                        var rawKey = cacheKey + __RAWSUFFIX;
                        var rawHtml = await _cacheService.GetAsync<string>(rawKey, true).ConfigureAwait(false);
                        if (!string.IsNullOrWhiteSpace(rawHtml))
                        {
                            try
                            {
                                _logger.LogInformation("Using cached raw HTML for {ReportType}...", typeof(T).Name);
                                doc = BuildHtmlDocumentFromRaw(rawHtml);
                                _telemetry.TrackCacheHit(dtoType, reportUrl, "RawHtml", null);
                                _logger.LogInformation("Successfully loaded cached raw HTML for {ReportType}...", typeof(T).Name);
                            }
                            catch (Exception ex)
                            {
                                _telemetry.TrackFailure(dtoType, reportUrl, "LoadCachedHtml", ex.GetType().Name);
                                _logger.LogError(ex, "Failed to load cached HTML for {ReportType}...", typeof(T).Name);
                                doc = null;
                            }
                        }
                    }

                    if (doc == null)
                    {
                        var lockResource = BuildReportLockResource(cacheKey);
                        await using var distLock = await _lockManager.AcquireLockAsync(lockResource, cancellationToken: CancellationToken.None);

                        if (distLock.IsAcquired)
                        {
                            var cachedResultsAfterLock = await _cacheService.GetAsync<List<T>>(cacheKey).ConfigureAwait(false);
                            if (cachedResultsAfterLock != null && cachedResultsAfterLock.Any())
                            {
                                _telemetry.TrackCacheHit(dtoType, reportUrl, "Results", cachedResultsAfterLock.Count);
                                _logger.LogInformation("Retrieving results from cache for {ReportType} after lock...", typeof(T).Name);
                                return cachedResultsAfterLock;
                            }

                            if (_settings.UseCachedHtml)
                            {
                                var rawKey = cacheKey + __RAWSUFFIX;
                                var rawHtml = await _cacheService.GetAsync<string>(rawKey, true).ConfigureAwait(false);
                                if (!string.IsNullOrWhiteSpace(rawHtml))
                                {
                                    try
                                    {
                                        _logger.LogInformation("Using cached raw HTML for {ReportType} after lock...", typeof(T).Name);
                                        doc = BuildHtmlDocumentFromRaw(rawHtml);
                                        _telemetry.TrackCacheHit(dtoType, reportUrl, "RawHtml", null);
                                        _logger.LogInformation("Successfully loaded cached raw HTML for {ReportType} after lock...", typeof(T).Name);
                                    }
                                    catch (Exception ex)
                                    {
                                        _telemetry.TrackFailure(dtoType, reportUrl, "LoadCachedHtmlAfterLock", ex.GetType().Name);
                                        _logger.LogError(ex, "Failed to load cached HTML for {ReportType} after lock...", typeof(T).Name);
                                        doc = null;
                                    }
                                }
                            }

                            _logger.LogInformation("Starting report retrieval for {ReportType}...", typeof(T).Name);

                            await _igSessionManagementService.WaitForLoginAsync();

                            if (doc == null)
                            {
                                _logger.LogInformation("Fetching {ReportType} report from {Url}", typeof(T).Name, reportUrl);

                                var swFetch = System.Diagnostics.Stopwatch.StartNew();
                                doc = await _igSessionManagementService.GetPageContent(reportUrl);
                                swFetch.Stop();

                                var bytes = IGDataProviderTelemetry.GetUtf8ByteCount(doc?.DocumentNode?.OuterHtml);
                                _telemetry.TrackFetch(dtoType, reportUrl, "GET", bytes, swFetch.ElapsedMilliseconds, doc != null);
                            }

                            List<T> itemsLocked;
                            try
                            {
                                var swParse = System.Diagnostics.Stopwatch.StartNew();
                                itemsLocked = await parse(doc);
                                swParse.Stop();

                                _telemetry.TrackParse(dtoType, reportUrl, itemsLocked?.Count ?? 0, swParse.ElapsedMilliseconds, itemsLocked != null);
                            }
                            catch (Exception ex)
                            {
                                _telemetry.TrackFailure(dtoType, reportUrl, "ParseReport", ex.GetType().Name);
                                throw;
                            }

                            if (linkBuilder != null)
                            {
                                foreach (var item in itemsLocked)
                                {
                                    item.Links = linkBuilder(item);
                                }
                            }

                            if (!string.IsNullOrEmpty(cacheKey) && cacheTTL != null && itemsLocked.Any() && cache.WriteThrough)
                            {
                                var rawTtl = TimeSpan.FromMinutes(15);

                                await _cacheService.SetAsync(cacheKey, itemsLocked, cacheTTL.Value).ConfigureAwait(false);

                                if (_settings.UseCachedHtml)
                                {
                                    var rawKey = cacheKey + __RAWSUFFIX;
                                    await _cacheService.SetAsync(rawKey, doc.DocumentNode.OuterHtml, rawTtl).ConfigureAwait(false);
                                }

                                _logger.LogInformation("Wrote cache for {ReportType} (key={CacheKey}, count={Count})", typeof(T).Name, cacheKey, itemsLocked.Count);
                            }

                            return itemsLocked;
                        }
                    }
                }
                else
                {
                    _logger.LogInformation("no-cache configured for report type {ReportType} from {Url}", typeof(T).Name, reportUrl);
                }
            }
            else
            {
                _logger.LogInformation("No cache key provided for report type {ReportType} from {Url}", typeof(T).Name, reportUrl);
            }

            _logger.LogInformation("Starting report retrieval for {ReportType}...", typeof(T).Name);

            await _igSessionManagementService.WaitForLoginAsync();

            if (doc == null)
            {
                _logger.LogInformation("Fetching {ReportType} report from {Url}", typeof(T).Name, reportUrl);

                var swFetch = System.Diagnostics.Stopwatch.StartNew();
                doc = await _igSessionManagementService.GetPageContent(reportUrl);
                swFetch.Stop();

                var bytes = IGDataProviderTelemetry.GetUtf8ByteCount(doc?.DocumentNode?.OuterHtml);
                _telemetry.TrackFetch(dtoType, reportUrl, "GET", bytes, swFetch.ElapsedMilliseconds, doc != null);
            }

            List<T> items;
            try
            {
                var swParse = System.Diagnostics.Stopwatch.StartNew();
                items = await parse(doc);
                swParse.Stop();

                _telemetry.TrackParse(dtoType, reportUrl, items?.Count ?? 0, swParse.ElapsedMilliseconds, items != null);
            }
            catch (Exception ex)
            {
                _telemetry.TrackFailure(dtoType, reportUrl, "ParseReport", ex.GetType().Name);
                throw;
            }

            if (linkBuilder != null)
            {
                foreach (var item in items)
                {
                    item.Links = linkBuilder(item);
                }
            }

            if (!string.IsNullOrEmpty(cacheKey) && cacheTTL != null && items.Any() && cache.WriteThrough && _cacheService != null)
            {
                var rawTtl = TimeSpan.FromMinutes(15);

                await _cacheService.SetAsync(cacheKey, items, cacheTTL.Value).ConfigureAwait(false);

                if (_settings.UseCachedHtml)
                {
                    var rawKey = cacheKey + __RAWSUFFIX;
                    await _cacheService.SetAsync(rawKey, doc.DocumentNode.OuterHtml, rawTtl).ConfigureAwait(false);
                }

                _logger.LogInformation("Wrote cache for {ReportType} (key={CacheKey}, count={Count})", typeof(T).Name, cacheKey, items.Count);
            }

            return items;
        }

        private static HtmlDocument BuildHtmlDocumentFromRaw(string raw)
        {
            var decoded = DecodePossiblyEscapedHtml(raw);
            var doc = new HtmlDocument();
            doc.LoadHtml(decoded);
            return doc;
        }

        private static string DecodePossiblyEscapedHtml(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            var s = input.Trim();

            if (s.IndexOf("<html", StringComparison.OrdinalIgnoreCase) >= 0 &&
                s.IndexOf("\\u003C", StringComparison.Ordinal) < 0)
            {
                return s;
            }

            if ((s.StartsWith("\"") && s.EndsWith("\"")) || (s.StartsWith("'") && s.EndsWith("'")))
            {
                s = s[1..^1];
            }

            s = s.Replace("\\u003C", "<")
                 .Replace("\\u003E", ">")
                 .Replace("\\u0026", "&")
                 .Replace("\\u0022", "\"")
                 .Replace("\\u0027", "'")
                 .Replace("\\/", "/")
                 .Replace("\\\\", "\\");

            return s;
        }

        public async Task<string> GetData(string reportUrl, string? cacheKey = null, TimeSpan? cacheTTL = null)
        {
            var cache = _cacheOptions.Current;

            HtmlDocument? doc = null;
            
            _logger.LogInformation("Starting retrieval of raw reponse for {reportUrl}", reportUrl);

            cacheTTL = cacheTTL ?? TimeSpan.FromMinutes(_settings.Cache.Default_TTL_Mins);

            if (!string.IsNullOrEmpty(cacheKey))
            {
                if (!cache.NoCache)
                {
                    var rawKey = cacheKey + __RAWSUFFIX;
                    var rawHtml = await _cacheService.GetAsync<string>(rawKey, true).ConfigureAwait(false);
                    if (!string.IsNullOrWhiteSpace(rawHtml))
                    {
                        try
                        {
                            _logger.LogInformation("Using cached raw HTML for {reportUrl}", reportUrl);
                            doc = BuildHtmlDocumentFromRaw(rawHtml);

                            _logger.LogInformation("Successfully loaded cached raw HTML for {reportUrl}", reportUrl);
                        }
                        catch (Exception)
                        {
                            _logger.LogError("Failed to load cached HTML for {reportUrl}", reportUrl);
                            doc = null;
                        }
                    }

                    if (doc == null)
                    {
                        var lockResource = BuildReportLockResource(cacheKey);
                        await using var distLock = await _lockManager.AcquireLockAsync(lockResource, cancellationToken: CancellationToken.None);

                        if (distLock.IsAcquired)
                        {
                            var rawKeyAfterLock = cacheKey + __RAWSUFFIX;
                            var rawHtmlAfterLock = await _cacheService.GetAsync<string>(rawKeyAfterLock, true).ConfigureAwait(false);
                            if (!string.IsNullOrWhiteSpace(rawHtmlAfterLock))
                            {
                                try
                                {
                                    _logger.LogInformation("Using cached raw HTML for {reportUrl} after lock", reportUrl);
                                    doc = BuildHtmlDocumentFromRaw(rawHtmlAfterLock);

                                    _logger.LogInformation("Successfully loaded cached raw HTML for {reportUrl} after lock", reportUrl);
                                }
                                catch (Exception)
                                {
                                    _logger.LogError("Failed to load cached HTML for {reportUrl} after lock", reportUrl);
                                    doc = null;
                                }
                            }

                            await _igSessionManagementService.WaitForLoginAsync();

                            _logger.LogInformation("Starting report retrieval for {reportUrl}", reportUrl);

                            await _igSessionManagementService.WaitForLoginAsync();

                            _logger.LogInformation("Fetching report from {Url}", reportUrl);
                            if (doc == null) doc = await _igSessionManagementService.GetPageContent(reportUrl);

                            if (!string.IsNullOrEmpty(cacheKey) && cacheTTL != null && doc != null && doc.DocumentNode != null && doc.DocumentNode.OuterHtml.Length != 0 && cache.WriteThrough)
                            {
                                await _cacheService!.SetAsync(cacheKey + __RAWSUFFIX, doc.DocumentNode.OuterHtml, cacheTTL.Value).ConfigureAwait(false);
                            }

                            if (doc != null)
                            {
                                return doc.DocumentNode.OuterHtml;
                            }
                            else
                            {
                                _logger.LogError("Failed to retrieve data from {Url}", reportUrl);
                                return string.Empty;
                            }
                        }
                    }
                }
            }

            await _igSessionManagementService.WaitForLoginAsync();

            _logger.LogInformation("Starting report retrieval for {reportUrl}", reportUrl);

            await _igSessionManagementService.WaitForLoginAsync();

            _logger.LogInformation("Fetching report from {Url}", reportUrl);
            if (doc == null) doc = await _igSessionManagementService.GetPageContent(reportUrl);

            if (!string.IsNullOrEmpty(cacheKey) && cacheTTL != null && doc != null && doc.DocumentNode != null && doc.DocumentNode.OuterHtml.Length != 0 && cache.WriteThrough)
            {
                var rawKey = cacheKey + __RAWSUFFIX;
                var rawHtml = doc.DocumentNode.OuterHtml;
                await _cacheService!.SetAsync(rawKey, rawHtml, cacheTTL.Value).ConfigureAwait(false);
            }

            if (doc != null)
            {
                return doc.DocumentNode.OuterHtml;
            }
            else
            {
                _logger.LogError("Failed to retrieve data from {Url}", reportUrl);
                return string.Empty;
            }
        }

        private static string BuildReportLockResource(string cacheKey)
        {
            return $"ig-report:{cacheKey}";
        }
    }
}
