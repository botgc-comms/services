using BOTGC.POS.Models;

namespace BOTGC.POS.Services;

public interface IProductService
{
    Task<IReadOnlyList<Product>> GetTop20Async();
    Task<IReadOnlyList<Product>> SearchAsync(string query, int take = 20);
    Task<Product?> GetAsync(Guid id);
}

