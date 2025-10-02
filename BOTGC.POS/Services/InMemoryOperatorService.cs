using BOTGC.POS.Models;

namespace BOTGC.POS.Services;

public class InMemoryOperatorService : IOperatorService
{
    private readonly List<Operator> _ops = new()
    {
        new Operator(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1"), "Alice"),
        new Operator(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa2"), "Ben"),
        new Operator(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa3"), "Chloe"),
        new Operator(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa4"), "Dylan"),
        new Operator(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa5"), "Ellie")
    };

    public Task<IReadOnlyList<Operator>> GetAllAsync() => Task.FromResult((IReadOnlyList<Operator>)_ops);
    public Task<Operator?> GetAsync(Guid id) => Task.FromResult(_ops.FirstOrDefault(o => o.Id == id));
}


public class InMemoryProductService : IProductService
{
    private readonly List<Product> _products;
    private readonly List<Guid> _popularIds;

    public InMemoryProductService()
    {
        _products = new List<Product>
        {
            new Product(Guid.NewGuid(), "Bass", "Beer", false),
            new Product(Guid.NewGuid(), "Level Head", "Beer", false),
            new Product(Guid.NewGuid(), "Estrella Galicia 0%", "Beer", false),
            new Product(Guid.NewGuid(), "Carling Lager", "Beer", false),
            new Product(Guid.NewGuid(), "San Miguel", "Beer", false),
            new Product(Guid.NewGuid(), "Aspall", "Cider", false),
            new Product(Guid.NewGuid(), "Madri", "Beer", false),
            new Product(Guid.NewGuid(), "Guinness", "Beer", false),
            new Product(Guid.NewGuid(), "BlackHole Guest Ale", "Beer", false),
            new Product(Guid.NewGuid(), "Casa Santiago Merlot 750ml Bottle", "Wine", false),
            new Product(Guid.NewGuid(), "Brookford Shiraz Cabernet", "Wine", false),
            new Product(Guid.NewGuid(), "Despacito Malbec 750ml Bottle", "Wine", false),
            new Product(Guid.NewGuid(), "Pepsi Max Can", "Soft Drink", false),
            new Product(Guid.NewGuid(), "Guinness 0.0% 538ml Can", "Beer", false),
            new Product(Guid.NewGuid(), "Coca Cola Can", "Soft Drink", false),
            new Product(Guid.NewGuid(), "Orange Tango", "Soft Drink", false),
            new Product(Guid.NewGuid(), "Di Maria Rose Prosecco 200ml Bottle", "Wine", false)
        };

        // track popular IDs in descending order of actual wastage counts
        _popularIds = _products.Select(p => p.Id).ToList();
    }

    public Task<IReadOnlyList<Product>> GetTop20Async()
    {
        // Return only our wastage-driven list
        var top = _products.Take(20).ToList();
        return Task.FromResult((IReadOnlyList<Product>)top);
    }

    public Task<Product?> GetAsync(Guid id)
    {
        return Task.FromResult(_products.FirstOrDefault(p => p.Id == id));
    }

    public Task<IReadOnlyList<Product>> SearchAsync(string query, int take = 20)
    {
        query = query.Trim().ToLowerInvariant();
        var res = _products
            .Where(p => p.Name.ToLowerInvariant().Contains(query) ||
                        p.Category.ToLowerInvariant().Contains(query))
            .Take(take)
            .ToList();

        return Task.FromResult((IReadOnlyList<Product>)res);
    }
}

public class InMemoryReasonService : IReasonService
{
    private readonly List<Reason> _reasons = new()
    {
        new Reason(Guid.NewGuid(), "Drip Trays", 1m),
        new Reason(Guid.NewGuid(), "Pipes Cleaned", 5m),
        new Reason(Guid.NewGuid(), "Breakage", null),
        new Reason(Guid.NewGuid(), "Incorrect Order", null),
        new Reason(Guid.NewGuid(), "Expired Stock", null)
    };

    public Task<IReadOnlyList<Reason>> GetAllAsync() => Task.FromResult((IReadOnlyList<Reason>)_reasons);
    public Task<Reason?> GetAsync(Guid id) => Task.FromResult(_reasons.FirstOrDefault(r => r.Id == id));
}

public class InMemoryWasteService : IWasteService
{
    private WasteSheet _today = new() { Date = DateTime.UtcNow.Date };

    public Task<WasteSheet> GetTodayAsync()
    {
        if (_today.Date != DateTime.UtcNow.Date)
            _today = new WasteSheet { Date = DateTime.UtcNow.Date };
        return Task.FromResult(_today);
    }

    public Task AddAsync(WasteEntry entry)
    {
        _today.Entries.Add(entry);
        return Task.CompletedTask;
    }

    public Task SubmitTodayAsync()
    {
        _today.Submitted = true;
        _today = new WasteSheet { Date = DateTime.UtcNow.Date };
        return Task.CompletedTask;
    }
}


