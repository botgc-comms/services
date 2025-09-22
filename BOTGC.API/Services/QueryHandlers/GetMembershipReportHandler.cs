using BOTGC.API.Common;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetMembershipReportHandler(IOptions<AppSettings> settings,
                                            ILogger<GetMembershipReportHandler> logger,
                                            IDataProvider dataProvider,
                                            IReportParser<MemberDto> reportParser) : QueryHandlerBase<GetMembershipReportQuery, List<MemberDto>>
    {
        private const string __CACHE_KEY = "Management_Report";

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<GetMembershipReportHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParser<MemberDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));

        private static string NormaliseUrl(string raw)
        {
            var u = new Uri(raw, UriKind.Absolute);
            var b = new UriBuilder(u)
            {
                Scheme = u.Scheme.ToLowerInvariant(),
                Host = u.Host.ToLowerInvariant(),
                Port = (u.Scheme == "https" && u.Port == 443) || (u.Scheme == "http" && u.Port == 80) ? -1 : u.Port,
                Path = string.IsNullOrEmpty(u.AbsolutePath) ? "/" : u.AbsolutePath.TrimEnd('/'),
                Query = string.Join("&",
                    u.Query.TrimStart('?')
                     .Split('&', StringSplitOptions.RemoveEmptyEntries)
                     .Select(p => p.Split('=', 2))
                     .OrderBy(kv => kv[0], StringComparer.OrdinalIgnoreCase)
                     .Select(kv => kv.Length == 2 ? $"{kv[0]}={kv[1]}" : kv[0]))
            };
            return b.Uri.ToString();

        }
        private static string BuildCacheKey(string prefix, string url, DateTime now)
        {
            var urlHash = Sha256Hex(Encoding.UTF8.GetBytes(url)).Substring(0, 16);
            return $"{prefix}:{now:yyyy-MM-dd}:{urlHash}";
        }

        private static string Sha256Hex(ReadOnlySpan<byte> data)
        {
            Span<byte> hash = stackalloc byte[32];
            SHA256.HashData(data, hash);
            var sb = new StringBuilder(64);
            foreach (var b in hash) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        public async override Task<List<MemberDto>> Handle(GetMembershipReportQuery request, CancellationToken cancellationToken)
        {
            var baseUrl = _settings.IG.BaseUrl;
            var rel = _settings.IG.Urls.MembershipReportingUrl;
            var reportUrl = NormaliseUrl($"{baseUrl}{rel}");

            var now = DateTime.Now;
            var midnight = now.Date.AddDays(1);
            var timeUntilMidnight = midnight - now;

            var cacheKey = BuildCacheKey("Management_Report", reportUrl, now);

            _logger.LogInformation("MembershipReport: Url={Url} CacheKey={CacheKey} TTL={TTL}", reportUrl, cacheKey, timeUntilMidnight);

            var members = await _dataProvider.GetData<MemberDto>(
                reportUrl,
                _reportParser,
                cacheKey,
                timeUntilMidnight
            );

            _logger.LogInformation("MembershipReport: Retrieved {Count} rows.", members.Count);

            members = members.Where(m => m.MembershipCategory?.ToLower() != "tests").ToList();

            return members;
        }
    }
}
