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

            var colourMap = await cache.GetAsync<Dictionary<int, string>>(COLOUR_MAP_CACHE_KEY).ConfigureAwait(false)
                            ?? new Dictionary<int, string>();

            var changed = false;

            foreach (var op in tillOperators)
            {
                var key = op.Id;
                if (key == 0 && !string.IsNullOrWhiteSpace(op.Name))
                {
                    key = StableKeyFromName(op.Name);
                }

                if (key == 0) key = -1;

                if (!colourMap.TryGetValue(key, out var hex))
                {
                    hex = GenerateNiceColour(key, op.Name);
                    colourMap[key] = hex;
                    changed = true;
                }

                op.ColorHex = hex;
            }

            if (changed)
            {
                await cache.SetAsync(COLOUR_MAP_CACHE_KEY, colourMap, TimeSpan.FromDays(3650)).ConfigureAwait(false);
                _logger.LogInformation("Till operator colour map updated with {Count} entries.", colourMap.Count);
            }

            return tillOperators;
        }

        private static int StableKeyFromName(string name)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(name.Trim()));
            // Fold first 4 bytes into a positive 31-bit int
            var v = (bytes[0] << 24) | (bytes[1] << 16) | (bytes[2] << 8) | bytes[3];
            if (v < 0) v = ~v;
            if (v == 0) v = 1;
            return v;
        }

        private static string GenerateNiceColour(int id, string? name)
        {
            using var sha = SHA256.Create();
            var key = $"{id}|{name}";
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(key));

            var u64 = BitConverter.ToUInt64(hash, 0);
            var u32a = BitConverter.ToUInt32(hash, 8);
            var u32b = BitConverter.ToUInt32(hash, 12);

            double hue = u64 / (double)ulong.MaxValue;
            hue = (hue + 0.38196601125) % 1.0;

            const int bins = 18;
            var binCentre = Math.Round(hue * bins) / bins;
            var jitter = (u32a / (double)uint.MaxValue - 0.5) * (1.0 / bins);
            hue = (binCentre + jitter + 1.0) % 1.0;

            double s = 0.55 + (u32a / (double)uint.MaxValue) * 0.30;
            double l = 0.60 + (u32b / (double)uint.MaxValue) * 0.12;

            (byte r, byte g, byte b) = HslToRgb(hue, s, l);
            return $"#{r:X2}{g:X2}{b:X2}";
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

            double Chan(double t)
            {
                if (t < 0) t += 1;
                if (t > 1) t -= 1;
                if (t < 1.0 / 6.0) return p + (q - p) * 6 * t;
                if (t < 1.0 / 2.0) return q;
                if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6;
                return p;
            }

            var r = (byte)Math.Round(Chan(h + 1.0 / 3.0) * 255.0);
            var g = (byte)Math.Round(Chan(h) * 255.0);
            var b = (byte)Math.Round(Chan(h - 1.0 / 3.0) * 255.0);
            return (r, g, b);
        }
    }
}
