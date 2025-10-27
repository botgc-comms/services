using System.Text;
using System.Linq;
using System.Security.Cryptography;
using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using BOTGC.API.Services.Queries;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BOTGC.API.Services.QueryHandlers;

public sealed class GetWastageProductsHandler(IOptions<AppSettings> settings,
                                              IMediator mediator,
                                              ILogger<GetWastageProductsHandler> logger,
                                              IServiceScopeFactory scopeFactory)
    : QueryHandlerBase<GetWastageProductsQuery, List<WastageProductDto>>
{
    private const string __CACHE_KEY = "Wastage_Products";

    private readonly AppSettings _settings = settings?.Value ?? throw new ArgumentNullException(nameof(settings));
    private readonly IMediator _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    private readonly ILogger<GetWastageProductsHandler> _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));

    public async override Task<List<WastageProductDto>> Handle(GetWastageProductsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var cache = scope.ServiceProvider.GetRequiredService<ICacheService>();

            var cached = await cache.GetAsync<List<WastageProductDto>>(__CACHE_KEY).ConfigureAwait(false);
            if (cached != null)
            {
                _logger.LogInformation("Wastage products retrieved from cache.");
                return cached;
            }

            var stockItems = await _mediator.Send(new GetStockLevelsQuery(), cancellationToken);
            var tillProducts = await _mediator.Send(new GetTillProductsQuery(), cancellationToken);

            if (tillProducts == null || tillProducts.Count == 0)
            {
                _logger.LogWarning("No till products returned for wastage processing.");
                return new List<WastageProductDto>();
            }

            var stockIndex = BuildStockIndex(stockItems);

            _logger.LogInformation("Building wastage products from {Count} till products.", tillProducts.Count);

            var map = new Dictionary<string, Accumulator>(StringComparer.OrdinalIgnoreCase);

            foreach (var p in tillProducts)
            {
                var components = p.Components ?? new List<TillProductStockComponentDto>();
                if (components.Count == 0)
                {
                    continue;
                }

                string name;
                List<TillProductStockComponentDto> componentSet;

                if (components.Count == 1)
                {
                    var c = components[0];
                    if (c.StockId.HasValue && stockIndex.TryGetValue(c.StockId.Value, out var s) && !string.IsNullOrWhiteSpace(s.Name))
                    {
                        name = Canonicalise(s.Name);
                    }
                    else
                    {
                        name = Canonicalise(c.Name ?? p.ProductName ?? string.Empty);
                    }
                    componentSet = new List<TillProductStockComponentDto> { c };
                }
                else
                {
                    name = Canonicalise(p.ProductName ?? string.Empty);
                    componentSet = components;
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (!map.TryGetValue(name, out var acc))
                {
                    acc = new Accumulator(name);
                    map[name] = acc;
                }

                foreach (var c in componentSet)
                {
                    if (c.StockId is null)
                    {
                        continue;
                    }

                    var sid = c.StockId.Value;

                    if (!acc.SeenStockIds.Add(sid))
                    {
                        continue;
                    }

                    if (stockIndex.TryGetValue(sid, out var s))
                    {
                        acc.StockItems.Add(CloneWithUsageQuantity(s, c.Quantity));
                    }
                    else
                    {
                        acc.StockItems.Add(new StockItemDto
                        {
                            Id = sid,
                            Name = c.Name ?? string.Empty,
                            Unit = c.Unit ?? string.Empty,
                            Quantity = c.Quantity
                        });
                    }
                }
            }

            var referenced = new HashSet<int>(map.Values.SelectMany(a => a.StockItems).Select(si => si.Id));
            foreach (var s in stockIndex.Values)
            {
                if (s == null || s.Id <= 0) continue;
                if (referenced.Contains(s.Id)) continue;

                var key = Canonicalise(s.Name ?? string.Empty);
                if (!map.TryGetValue(key, out var acc))
                {
                    acc = new Accumulator(key);
                    map[key] = acc;
                }

                if (acc.SeenStockIds.Add(s.Id))
                {
                    acc.StockItems.Add(CloneWithUsageQuantity(s, null));
                }
            }

            var result = map.Values
                            .OrderBy(a => a.DisplayName, StringComparer.OrdinalIgnoreCase)
                            .Select(a =>
                            {
                                var ids = a.StockItems.Select(si => si.Id);
                                return new WastageProductDto
                                {
                                    Id = ComputeStableId(a.DisplayName, ids),
                                    Name = a.DisplayName,
                                    StockItems = a.StockItems
                                };
                            })
                            .ToList();

            await cache.SetAsync(__CACHE_KEY, result, TimeSpan.FromMinutes(_settings.Cache.MediumTerm_TTL_mins)).ConfigureAwait(false);

            _logger.LogInformation("Built {Count} unique wastage products.", result.Count);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to build wastage products.");
            return new List<WastageProductDto>();
        }
    }

    private static Dictionary<int, StockItemDto> BuildStockIndex(List<StockItemDto>? stockItems)
    {
        var index = new Dictionary<int, StockItemDto>(stockItems?.Count ?? 0);
        if (stockItems == null)
        {
            return index;
        }

        foreach (var s in stockItems)
        {
            if (s != null && s.Id > 0 && !index.ContainsKey(s.Id))
            {
                index[s.Id] = s;
            }
        }

        return index;
    }

    private static StockItemDto CloneWithUsageQuantity(StockItemDto s, decimal? usageQuantity)
    {
        return new StockItemDto
        {
            Id = s.Id,
            ExternalId = s.ExternalId,
            Name = s.Name,
            MinAlert = s.MinAlert,
            MaxAlert = s.MaxAlert,
            IsActive = s.IsActive,
            TillStockDivisionId = s.TillStockDivisionId,
            Unit = s.Unit,
            Quantity = usageQuantity,
            TillStockRoomId = s.TillStockRoomId,
            Division = s.Division,
            Value = s.Value,
            TotalQuantity = s.TotalQuantity,
            TotalValue = s.TotalValue,
            OneQuantity = s.OneQuantity,
            OneValue = s.OneValue,
            TwoQuantity = s.TwoQuantity,
            TwoValue = s.TwoValue
        };
    }

    private static string Canonicalise(string s)
    {
        var trimmed = s.Trim();
        var sb = new StringBuilder(trimmed.Length);
        var prevSpace = false;

        foreach (var ch in trimmed)
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!prevSpace)
                {
                    sb.Append(' ');
                    prevSpace = true;
                }
            }
            else
            {
                sb.Append(ch);
                prevSpace = false;
            }
        }

        return sb.ToString();
    }

    private static string ComputeStableId(string name, IEnumerable<int> stockIds)
    {
        var key = $"{name}|{string.Join(",", stockIds.OrderBy(i => i))}";
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(key));
        var sb = new StringBuilder(32);
        for (int i = 0; i < 16; i++)
        {
            sb.Append(hash[i].ToString("x2"));
        }
        return sb.ToString();
    }

    private sealed class Accumulator
    {
        public string DisplayName { get; }
        public HashSet<int> SeenStockIds { get; }
        public List<StockItemDto> StockItems { get; }

        public Accumulator(string displayName)
        {
            DisplayName = displayName;
            SeenStockIds = new HashSet<int>();
            StockItems = new List<StockItemDto>();
        }
    }
}
