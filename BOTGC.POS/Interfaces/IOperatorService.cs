using BOTGC.POS.Models;

namespace BOTGC.POS.Services;

public interface IOperatorService
{
    Task<IReadOnlyList<Operator>> GetAllAsync();
    Task<Operator?> GetAsync(Guid id);
}

