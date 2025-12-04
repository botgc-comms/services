using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;
using BOTGC.POS.Models;

namespace BOTGC.POS.Services;

public sealed class HttpProductService : IProductService
{
    private static readonly string[] s_top20Names =
    {
        "^.*?Bass.*?$",
        "^.*Level\\sHead.*$",
        "^.*Estrella\\sGalicia.*$",
        "^.*Carling.*$",
        "^.*San\\sMiguel.*$",
        "^.*Aspall.*$",
        "^.*Madri.*$",
        "^.*Guinness.*$",
        "^.*Guest\\sAle.*$",
        "^.*Casa\\sSantiago\\sMerlot.*$",
        "^.*Brookford\\sShiraz.*$",
        "^.*Despacito\\sMalbec.*$",
        "^.*Pepsi\\sMax.*$",
        "^.*Coca\\sCola.*$",
        "^.*Prosecco.*$"
    };

    private static readonly Regex s_top20Combined = BuildAlternationWithNamedGroups(s_top20Names);

    private readonly IHttpClientFactory _factory;

    // Legacy stock cache (kept for fallback GetAsync where needed)
    private List<Product>? _stockCache;
    private List<(Product P, string NameLower, string CategoryLower)>? _stockSearch;
    private Dictionary<Guid, Product>? _stockById;
    private DateTimeOffset _stockCacheUntil = DateTimeOffset.MinValue;

    // Wastage product cache (primary)
    private List<Product>? _wCache;
    private List<(Product P, string NameLower, string CategoryLower)>? _wCacheSearch;
    private Dictionary<Guid, Product>? _wById;
    private Dictionary<Guid, ProductDetails>? _wDetailsById;
    private DateTimeOffset _wCacheUntil = DateTimeOffset.MinValue;

    public HttpProductService(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<Product>> GetTop20Async()
    {
        await EnsureWastageCacheAsync();
        var products = _wCache!;
        var results = new Product?[s_top20Names.Length];
        var chosen = new HashSet<Guid>();

        foreach (var p in products)
        {
            var m = s_top20Combined.Match(p.Name);
            if (!m.Success) continue;

            for (int i = 0; i < results.Length; i++)
            {
                var g = m.Groups["p" + i];
                if (g.Success && results[i] is null && chosen.Add(p.Id))
                {
                    results[i] = p;
                }
            }

            if (chosen.Count == results.Length) break;
        }

        return results.Where(x => x is not null).Cast<Product>().ToList();
    }

    public async Task<Product?> GetAsync(Guid id)
    {
        await EnsureWastageCacheAsync();
        if (_wById!.TryGetValue(id, out var p)) return p;

        await EnsureStockCacheAsync();
        return _stockById!.TryGetValue(id, out var legacy) ? legacy : null;
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(string query, int take = 20)
    {
        await EnsureWastageCacheAsync();

        var q = query.AsSpan().Trim();
        var res = new List<Product>(Math.Min(take, 32));

        foreach (var row in _wCacheSearch!)
        {
            if (row.NameLower.AsSpan().IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0 ||
                row.CategoryLower.AsSpan().IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                res.Add(row.P);
                if (res.Count >= take) break;
            }
        }

        return res;
    }

    public async Task<ProductDetails?> GetDetailsAsync(Guid id)
    {
        await EnsureWastageCacheAsync();
        return _wDetailsById != null && _wDetailsById.TryGetValue(id, out var d) ? d : null;
    }

    private async Task EnsureStockCacheAsync()
    {
        if (_stockCache is not null && DateTimeOffset.UtcNow < _stockCacheUntil) return;

        var client = _factory.CreateClient("Api");
        var api = await client.GetFromJsonAsync<List<ApiStockItem>>("api/stock/stockLevels") ?? new List<ApiStockItem>();

        var products = api
            .Where(s => s.IsActive == true)
            .Select(s =>
            {
                var id = StableGuidFor("botgc.pos.stock:", s.Id.ToString());
                var igid = s.Id;
                var unit = s.Unit ?? string.Empty;
                var name = (s.Name ?? $"Stock {s.Id}").Trim();
                var category = (s.Division ?? string.Empty).Trim();
                return new Product(id, name, category, false, igid, unit);
            })
            .GroupBy(p => p.Name.ToLowerInvariant())
            .Select(g => g.First())
            .ToList();

        _stockSearch = products
            .Select(p => (p, p.Name.ToLowerInvariant(), p.Category.ToLowerInvariant()))
            .ToList();

        _stockCache = products;
        _stockById = _stockCache.ToDictionary(p => p.Id, p => p);
        _stockCacheUntil = DateTimeOffset.UtcNow.AddMinutes(5);
    }

    private async Task EnsureWastageCacheAsync()
    {
        if (_wCache is not null && DateTimeOffset.UtcNow < _wCacheUntil) return;

        var client = _factory.CreateClient("Api");
        var api = await client.GetFromJsonAsync<List<ApiWastageProduct>>("api/stock/wastesheet/products")
                  ?? new List<ApiWastageProduct>();

        _wDetailsById = new Dictionary<Guid, ProductDetails>(api.Count);
        _wById = new Dictionary<Guid, Product>(api.Count);
        _wCache = new List<Product>(api.Count);

        foreach (var w in api)
        {
            // Stable client Guid based on server id (or name if id is blank)
            var rawKey = string.IsNullOrWhiteSpace(w.Id) ? (w.Name ?? Guid.NewGuid().ToString()) : w.Id!;
            var pid = StableGuidFor("botgc.pos.wastage:", rawKey);

            var name = (w.Name ?? "Unnamed").Trim();
            var comps = w.StockItems ?? new List<ApiWastageStockItem>(0);

            long igid;
            string unit;
            string finalName;
            string category;

            if (comps.Count == 1)
            {
                var c = comps[0];
                igid = c.Id;
                unit = (c.Unit ?? string.Empty).Trim();
                finalName = string.IsNullOrWhiteSpace(c.Name) ? name : c.Name!.Trim();
                category = ToTitle(c.Division ?? string.Empty).Trim();
            }
            else
            {
                igid = 0;
                unit = DeriveCompositeUnit(comps);
                finalName = name;
                category = DeriveCompositeCategory(comps);
            }

            var product = new Product(pid, finalName, category, false, igid, unit);
            _wById[pid] = product;
            _wCache.Add(product);

            var details = new ProductDetails(
                pid,
                finalName,
                category,
                igid,
                unit,
                comps.Select(c => new ProductComponent(
                    c.Id,
                    c.Name ?? string.Empty,
                    c.Unit ?? string.Empty,
                    c.Quantity,
                    c.Division ?? string.Empty)).ToList()
            );

            _wDetailsById[pid] = details;
        }

        // Do NOT de-dupe by name for wastage products; different compositions may share a name.

        _wCacheSearch = _wCache
            .Select(p => (p, p.Name.ToLowerInvariant(), p.Category.ToLowerInvariant()))
            .ToList();

        _wCacheUntil = DateTimeOffset.UtcNow.AddMinutes(5);
    }

    private static string DeriveCompositeUnit(List<ApiWastageStockItem> comps)
    {
        var pour = comps.FirstOrDefault(si =>
        {
            var u = si.Unit ?? string.Empty;
            return u.Contains("pint", StringComparison.OrdinalIgnoreCase)
                || u.Contains("ml", StringComparison.OrdinalIgnoreCase)
                || u.Contains("litre", StringComparison.OrdinalIgnoreCase)
                || u.Contains("liter", StringComparison.OrdinalIgnoreCase)
                || u.Contains("measure", StringComparison.OrdinalIgnoreCase)
                || u.Contains("shot", StringComparison.OrdinalIgnoreCase);
        });

        return (pour?.Unit ?? string.Empty).Trim();
    }

    private static string DeriveCompositeCategory(List<ApiWastageStockItem> comps)
    {
        var set = comps
            .Select(c => (c.Division ?? string.Empty).Trim())
            .Where(s => s.Length > 0)
            .Select(s => s.ToUpperInvariant())
            .Distinct()
            .ToList();

        if (set.Count == 0) return "Uncategorised";
        if (set.Count == 1) return ToTitle(set[0]);

        if (set.Contains("SPIRITS") && (set.Contains("SOFT DRINKS") || set.Contains("MIXERS"))) return "Spirit & Mixer";
        if (set.Any(s => s.Contains("BEER"))) return "Beer";
        if (set.Contains("CIDER")) return "Cider";
        if (set.Any(s => s.Contains("WINE"))) return "Wine";

        return "Kitchen";
    }

    private static string ToTitle(string s)
    {
        return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(s.ToLowerInvariant());
    }

    private static Regex BuildAlternationWithNamedGroups(IEnumerable<string> patterns)
    {
        var parts = patterns.Select((pat, i) => $"(?<p{i}>{pat})").ToArray();
        var alternation = string.Join("|", parts);
        return new Regex(alternation, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    }

    private static Guid StableGuidFor(string prefix, string key)
    {
        using var md5 = MD5.Create();
        var bytes = Encoding.UTF8.GetBytes(prefix + key);
        var hash = md5.ComputeHash(bytes);
        return new Guid(hash);
    }

    private sealed record ApiStockItem(
        long Id,
        string? Name,
        int? MinAlert,
        int? MaxAlert,
        bool? IsActive,
        int? TillStockDivisionId,
        string? Unit,
        decimal? Quantity,
        int? TillStockRoomId,
        string? Division,
        decimal? Value,
        decimal? TotalQuantity,
        decimal? TotalValue,
        decimal? OneQuantity,
        decimal? OneValue,
        decimal? TwoQuantity,
        decimal? TwoValue
    );

    private sealed record ApiWastageProduct(
        string? Id,
        string? Name,
        List<ApiWastageStockItem>? StockItems
    );

    private sealed record ApiWastageStockItem(
        int Id,
        string? Name,
        string? Unit,
        string? Division,
        decimal? Quantity
    );
}
