using BOTGC.POS.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace BOTGC.POS.Services;

public sealed class HttpProductService : IProductService
{
    private static readonly string[] s_top20Names =
    {
        "^.*?Bass.*?$",
        "^.*Level\\sHead.*$",
        "^.*Estrellal\\sGalicia.*$",
        "^.*Carling.*$",
        "^.*San\\sMiguel.*$",
        "^.*Aspall.*$",
        "^.*Madri.*$",
        "^.*Guinness.*$",
        "^.*Guest\\sAle.*$",
        "^.*Casa\\sSantiago\\ssMerlot.*$",
        "^.*Brookford\\shiraz.*$",
        "^.*Despacito\\sMalbec.*$",
        "^.*Pepsi\\sMax.*$",
        "^.*Coca\\sCola.*$",
        "^.*Prosecco.*$"
    };

    // Compile once; do NOT escape – these are regex patterns already.
    private static readonly Regex[] Top20NamePatterns =
        s_top20Names
            .Select(p => new Regex(p, RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking))
            .ToArray();

    // Single combined regex with named groups lets us scan the product list ONCE.
    private static readonly Regex s_top20Combined =
        BuildAlternationWithNamedGroups(s_top20Names);

    private readonly IHttpClientFactory _factory;
    private List<Product>? _cache;
    private List<(Product P, string NameLower, string CategoryLower)>? _cacheSearch;
    private Dictionary<Guid, Product>? _byId;
    private DateTimeOffset _cacheUntil = DateTimeOffset.MinValue;

    public HttpProductService(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<Product>> GetTop20Async()
    {
        await EnsureCacheAsync();
        var products = _cache!;
        var results = new Product?[s_top20Names.Length];
        var chosen = new HashSet<Guid>();

        // Single pass over products; use named groups to map which pattern matched.
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
        await EnsureCacheAsync();
        return _byId!.TryGetValue(id, out var p) ? p : null;
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(string query, int take = 20)
    {
        await EnsureCacheAsync();
        var q = query.AsSpan().Trim();

        // Avoid repeated ToLowerInvariant allocations; use OrdinalIgnoreCase IndexOf on cached lower strings.
        var res = new List<Product>(Math.Min(take, 32));

        foreach (var row in _cacheSearch!)
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

    private async Task EnsureCacheAsync()
    {
        if (_cache is not null && DateTimeOffset.UtcNow < _cacheUntil) return;

        var client = _factory.CreateClient("Api");
        var api = await client.GetFromJsonAsync<List<ApiStockItem>>("api/stock/stockLevels") ?? new List<ApiStockItem>();

        var products = api
            .Where(s => s.IsActive == true)
            .Select(s =>
            {
                var id = StableGuidFor(s.Id.ToString());
                var igid = s.Id;
                var unit = s.Unit;
                var name = s.Name?.Trim() ?? $"Stock {s.Id}";
                var category = (s.Division ?? string.Empty).Trim();
                return new Product(id, name, category, false, igid, unit);
            })
            .GroupBy(p => p.Name.ToLowerInvariant())
            .Select(g => g.First())
            .ToList();

        // Build search cache once to avoid per-query allocations and ToLower calls.
        _cacheSearch = products
            .Select(p => (p, p.Name.ToLowerInvariant(), p.Category.ToLowerInvariant()))
            .ToList();

        _cache = products;
        _byId = _cache.ToDictionary(p => p.Id, p => p);
        _cacheUntil = DateTimeOffset.UtcNow.AddMinutes(5);
    }

    private static Regex BuildAlternationWithNamedGroups(IEnumerable<string> patterns)
    {
        // Preserve order; each pattern gets its own named group.
        var parts = patterns
            .Select((pat, i) => $"(?<p{i}>{pat})")
            .ToArray();

        var alternation = string.Join("|", parts);
        return new Regex(alternation,
            RegexOptions.Compiled |
            RegexOptions.IgnoreCase |
            RegexOptions.CultureInvariant |
            RegexOptions.NonBacktracking);
    }

    private static Guid StableGuidFor(string externalId)
    {
        using var md5 = MD5.Create();
        var bytes = Encoding.UTF8.GetBytes("botgc.pos.stock:" + externalId);
        var hash = md5.ComputeHash(bytes);
        return new Guid(hash);
    }

    private sealed record ApiStockItem(
        long Id,
        string Name,
        int? MinAlert,
        int? MaxAlert,
        bool? IsActive,
        int? TillStockDivisionId,
        string Unit,
        decimal? Quantity,
        int? TillStockRoomId,
        string Division,
        decimal? Value,
        decimal? TotalQuantity,
        decimal? TotalValue,
        decimal? OneQuantity,
        decimal? OneValue,
        decimal? TwoQuantity,
        decimal? TwoValue
    );
}
