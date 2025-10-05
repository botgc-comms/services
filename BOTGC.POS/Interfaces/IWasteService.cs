using BOTGC.POS.Models;

namespace BOTGC.POS.Services;

public interface IWasteService
{
    Task<WasteSheet> GetTodayAsync();
    Task AddAsync(WasteEntry entry);
    Task SubmitTodayAsync();
    Task<bool> DeleteAsync(Guid id);
}

