using BOTGC.POS.Models;
using System.Security.Cryptography;
using System.Text;

namespace BOTGC.POS.Services;

public sealed class HttpOperatorService : IOperatorService
{
    private readonly IHttpClientFactory _factory;
    private List<Operator>? _cache;
    private Dictionary<Guid, Operator>? _byId;
    private DateTimeOffset _cacheUntil = DateTimeOffset.MinValue;

    public HttpOperatorService(IHttpClientFactory factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<Operator>> GetAllAsync()
    {
        await EnsureCacheAsync();
        return _cache!;
    }

    public async Task<Operator?> GetAsync(Guid id)
    {
        await EnsureCacheAsync();
        return _byId!.TryGetValue(id, out var op) ? op : null;
    }

    private async Task EnsureCacheAsync()
    {
        if (_cache is not null && DateTimeOffset.UtcNow < _cacheUntil) return;

        var client = _factory.CreateClient("Api");
        var api = await client.GetFromJsonAsync<List<ApiTillOperator>>("api/stock/tillOperators")
                  ?? new List<ApiTillOperator>();

        var ops = api
            .Where(o => o.IsActive)
            .Select(o => new Operator(StableGuidFor(o.Id.ToString()), o.Name, o.ColorHex))
            .OrderBy(o => o.DisplayName)
            .ToList();

        _cache = ops;
        _byId = _cache.ToDictionary(o => o.Id, o => o);
        _cacheUntil = DateTimeOffset.UtcNow.AddMinutes(5);
    }

    private static Guid StableGuidFor(string externalId)
    {
        using var md5 = MD5.Create();
        var bytes = Encoding.UTF8.GetBytes("botgc.pos.tillop:" + externalId);
        var hash = md5.ComputeHash(bytes);
        return new Guid(hash);
    }

    private sealed record ApiTillOperator(long Id, string Name, bool IsActive, string? ColorHex);
}
