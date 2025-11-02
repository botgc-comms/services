using System.Security.Cryptography;
using System.Text;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers
{
    public class GetTillOperatorsHandler(IOptions<AppSettings> settings,
                                         ILogger<GetTillOperatorsHandler> logger,
                                         IDataProvider dataProvider,
                                         IReportParser<TillOperatorDto> reportParser,
                                         IServiceScopeFactory scopeFactory)
        : QueryHandlerBase<GetTillOperatorsQuery, List<TillOperatorDto>>
    {
        private const string OPERATORS_CACHE_KEY = "TillOperators";
        private const string COLOUR_MAP_CACHE_KEY = "TillOperators:ColourMap";
        private const double MIN_HUE_SEPARATION_DEG = 22.0;
        private const double GOLDEN_ANGLE_TURN = 0.38196601125;

        private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
        private readonly ILogger<GetTillOperatorsHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        private readonly IDataProvider _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        private readonly IReportParser<TillOperatorDto> _reportParser = reportParser ?? throw new ArgumentNullException(nameof(reportParser));
        private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

        public async override Task<List<TillOperatorDto>> Handle(GetTillOperatorsQuery request, CancellationToken cancellationToken)
        {
            var reportUrl = $"{_settings.IG.BaseUrl}{_settings.IG.Urls.TillOperatorsReportUrl}";
            var tillOperators = await _dataProvider.GetData<TillOperatorDto>(
                reportUrl,
                _reportParser,
                OPERATORS_CACHE_KEY,
                TimeSpan.FromMinutes(_settings.Cache.MediumTerm_TTL_mins)
            );

            _logger.LogInformation("Retrieved {Count} till operator records.", tillOperators.Count);

            using var scope = _scopeFactory.CreateScope();
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var cachedMap = await cache.GetAsync<Dictionary<int, string>>(COLOUR_MAP_CACHE_KEY).ConfigureAwait(false) ?? new Dictionary<int, string>();

            // Build a new colour map with enforced hue separation, deterministic order.
            var newMap = new Dictionary<int, string>();
            var usedHues = new List<double>(); // 0..1
            foreach (var op in tillOperators.OrderBy(o => KeyFor(o)))
            {
                var key = KeyFor(op);
                var hex = GenerateDistinctPastel(key, op.Name, usedHues);
                newMap[key] = hex;
                op.ColorHex = hex;
            }

            var changed = !DictionaryEquals(cachedMap, newMap);
            if (changed)
            {
                await cache.SetAsync(COLOUR_MAP_CACHE_KEY, newMap, TimeSpan.FromDays(3650)).ConfigureAwait(false);
                _logger.LogInformation("Till operator colour map updated with {Count} entries.", newMap.Count);
            }

            return tillOperators;
        }

        private static int KeyFor(TillOperatorDto op)
        {
            var key = op.Id;
            if (key == 0 && !string.IsNullOrWhiteSpace(op.Name)) key = StableKeyFromName(op.Name);
            if (key == 0) key = -1;
            return key;
        }

        private static bool DictionaryEquals(Dictionary<int, string> a, Dictionary<int, string> b)
        {
            if (a.Count != b.Count) return false;
            foreach (var kv in a)
            {
                if (!b.TryGetValue(kv.Key, out var v)) return false;
                if (!StringComparer.OrdinalIgnoreCase.Equals(kv.Value, v)) return false;
            }
            return true;
        }

        private static int StableKeyFromName(string name)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(name.Trim()));
            var v = (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
            if (v < 0) v = ~v;
            if (v == 0) v = 1;
            return v;
        }

        private static string GenerateDistinctPastel(int id, string? name, List<double> usedHues)
        {
            using var sha = SHA256.Create();
            var key = $"{id}|{name}";
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(key));

            var u64 = BitConverter.ToUInt64(hash, 0);
            var u32a = BitConverter.ToUInt32(hash, 8);
            var u32b = BitConverter.ToUInt32(hash, 12);

            var h = u64 / (double)ulong.MaxValue;
            h = (h + GOLDEN_ANGLE_TURN) % 1.0;

            var s = 0.55 + (u32a / (double)uint.MaxValue) * 0.30;
            var l = 0.64 + (u32b / (double)uint.MaxValue) * 0.12;

            var minSep = MIN_HUE_SEPARATION_DEG / 360.0;
            var guard = 0;
            while (usedHues.Any(u => HueDistance(u, h) < minSep) && guard++ < 720)
            {
                h = (h + GOLDEN_ANGLE_TURN) % 1.0;
            }

            usedHues.Add(h);
            var (r, g, b) = HslToRgb(h, s, l);
            return $"#{r:X2}{g:X2}{b:X2}";
        }

        private static double HueDistance(double a, double b)
        {
            var d = Math.Abs(a - b) % 1.0;
            return d > 0.5 ? 1.0 - d : d;
        }

        private static (byte r, byte g, byte b) HslToRgb(double h, double s, double l)
        {
            if (s == 0)
            {
                var v = (byte)Math.Round(l * 255.0);
                return (v, v, v);
            }

            double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
            double p = 2 * l - q;

            static double Chan(double t, double p, double q)
            {
                if (t < 0) t += 1;
                if (t > 1) t -= 1;
                if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
                if (t < 1.0 / 2.0) return q;
                if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
                return p;
            }

            var r = (byte)Math.Round(Chan(h + 1.0 / 3.0, p, q) * 255.0);
            var g = (byte)Math.Round(Chan(h, p, q) * 255.0);
            var b = (byte)Math.Round(Chan(h - 1.0 / 3.0, p, q) * 255.0);
            return (r, g, b);
        }
    }
}
