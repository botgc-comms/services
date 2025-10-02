using BOTGC.POS.Models;

namespace BOTGC.POS.Services;

public interface IOperatorService
{
    Task<IReadOnlyList<Operator>> GetAllAsync();
    Task<Operator?> GetAsync(Guid id);
}

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetTop20Async();
    Task<IReadOnlyList<Product>> SearchAsync(string query, int take = 20);
    Task<Product?> GetAsync(Guid id);
}

public interface IReasonService
{
    Task<IReadOnlyList<Reason>> GetAllAsync();
    Task<Reason?> GetAsync(Guid id);
}

public interface IWasteService
{
    Task<WasteSheet> GetTodayAsync();
    Task AddAsync(WasteEntry entry);
    Task SubmitTodayAsync();
}
