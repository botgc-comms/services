using BOTGC.POS.Models;
using System.Security.Cryptography;
using System.Text;

namespace BOTGC.POS.Services;

public sealed class HttpProductService : IProductService
{
    private static readonly string[] s_top20Names =
    {
        "Bass",
        "Level Head",
        "Estrella Galicia 0%",
        "Carling Lager",
        "San Miguel",
        "Aspall",
        "Madri",
        "Guinness",
        "BlackHole Guest Ale",
        "Casa Santiago Merlot 750ml Bottle",
        "Brookford Shiraz Cabernet",
        "Despacito Malbec 750ml Bottle",
        "Pepsi Max Can",
        "Guinness 0.0% 538ml Can",
        "Coca Cola Can",
        "Orange Tango",
        "Di Maria Rose Prosecco 200ml Bottle"
    };

    private readonly IHttpClientFactory _factory;
    private List<Product>? _cache;
    private Dictionary<Guid, Product>? _byId;
    private DateTimeOffset _cacheUntil = DateTimeOffset.MinValue;

    public HttpProductService(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<Product>> GetTop20Async()
    {
        await EnsureCacheAsync();
        var byLowerName = _cache!.ToDictionary(p => p.Name.ToLowerInvariant(), p => p);
        var list = new List<Product>(capacity: s_top20Names.Length);

        foreach (var n in s_top20Names)
        {
            var key = n.ToLowerInvariant();
            if (byLowerName.TryGetValue(key, out var p))
            {
                list.Add(p);
            }
        }

        return list;
    }

    public async Task<Product?> GetAsync(Guid id)
    {
        await EnsureCacheAsync();
        return _byId!.TryGetValue(id, out var p) ? p : null;
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(string query, int take = 20)
    {
        await EnsureCacheAsync();
        query = query.Trim().ToLowerInvariant();
        var res = _cache!
            .Where(p => p.Name.ToLowerInvariant().Contains(query) ||
                        p.Category.ToLowerInvariant().Contains(query))
            .Take(take)
            .ToList();
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
            .OrderBy(p => p.Name)
            .ToList();

        _cache = products;
        _byId = _cache.ToDictionary(p => p.Id, p => p);
        _cacheUntil = DateTimeOffset.UtcNow.AddMinutes(5);
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
