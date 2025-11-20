using BOTGC.POS.Models;

namespace BOTGC.POS.Services;

public interface IReasonService
{
    Task<IReadOnlyList<Reason>> GetAllAsync();
    Task<Reason?> GetAsync(Guid id);
}
