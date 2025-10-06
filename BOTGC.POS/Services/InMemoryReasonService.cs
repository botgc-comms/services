using BOTGC.POS.Models;

namespace BOTGC.POS.Services;

public class InMemoryReasonService : IReasonService
{

    private static readonly string[] DraftOnlyBlock = new[] { "can", "bottle", "unit", "splash", "kg", "each", "packet" };
    private static readonly string[] UnitOnlyBlock = new[] { "pint" };

    private static readonly List<Reason> _reasons = new()
    {
        new Reason(Guid.Parse("f5c9a5a1-7f7e-4a54-9f2c-5a8d6c7b1e01"), "Drip Tray",          0.5m, DraftOnlyBlock),
        new Reason(Guid.Parse("b8f6b6a2-5a2b-4a3e-9e6a-2b7c9d1e2f02"), "Wrong Drink Given",  1m, UnitOnlyBlock),
        new Reason(Guid.Parse("3c1a2b3c-4d5e-6f70-8123-9a0b1c2d3e03"), "Spillage/Breakage",  1m, UnitOnlyBlock),
        new Reason(Guid.Parse("29d4e5f6-7a8b-49c0-91d2-3e4f5a6b7c04"), "Barrel Change",      6m,  DraftOnlyBlock),
        new Reason(Guid.Parse("6a7b8c9d-0e1f-42a3-b4c5-d6e7f8a9b005"), "Kitchen Usage",      1m, UnitOnlyBlock),
        new Reason(Guid.Parse("8e9f0a1b-2c3d-4e5f-9601-2a3b4c5d6e06"), "Poor Quality",       1m, UnitOnlyBlock),
        new Reason(Guid.Parse("7b6c5d4e-3f2a-41b0-8c9d-0a1b2c3d4e07"), "Past Sell By Date",  1m, UnitOnlyBlock),
        new Reason(Guid.Parse("1a2b3c4d-5e6f-4708-9012-3c4d5e6f7a08"), "Pipe Clean",         1m, DraftOnlyBlock),
        new Reason(Guid.Parse("9f8e7d6c-5b4a-43a2-9018-7c6b5a4d3e09"), "Bar Pull Through",   1m,  DraftOnlyBlock)
    };

    public Task<IReadOnlyList<Reason>> GetAllAsync() =>
        Task.FromResult((IReadOnlyList<Reason>)_reasons);

    public Task<Reason?> GetAsync(Guid id) =>
        Task.FromResult(_reasons.FirstOrDefault(r => r.Id == id));
}
