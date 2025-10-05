using BOTGC.POS.Models;

namespace BOTGC.POS.Services;

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

