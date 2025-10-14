using BOTGC.API.Dto;
using BOTGC.API.Interfaces;
using System.Collections.Concurrent;

namespace BOTGC.API.Services
{
    public sealed class BottleWeightService : IBottleWeightDataSource
    {
        private readonly ConcurrentDictionary<int, BottleWeightProfileDto> _profiles = new();

        public BottleWeightService()
        {
            SeedDefaults();
        }

        public BottleWeightService(IEnumerable<BottleWeightProfileDto> seed)
        {
            if (seed != null && seed.Any())
            {
                foreach (var p in seed) _profiles[p.StockItemId] = p;
            }
            else
            {
                SeedDefaults();
            }
        }

        public Task<BottleWeightProfileDto?> GetAsync(int stockItemId, CancellationToken cancellationToken)
        {
            _profiles.TryGetValue(stockItemId, out var profile);
            return Task.FromResult<BottleWeightProfileDto?>(profile);
        }

        public void Set(BottleWeightProfileDto profile) => _profiles[profile.StockItemId] = profile;

        public bool Remove(int stockItemId) => _profiles.TryRemove(stockItemId, out _);

        public void Clear() => _profiles.Clear();

        private void SeedDefaults()
        {
            // Example seeds matching your sample data
            _profiles[193] = new BottleWeightProfileDto(193, 400m, 1150m, 750m);
            _profiles[108] = new BottleWeightProfileDto(108, 400m, 900m, 750m);
            _profiles[207] = new BottleWeightProfileDto(207, 400m, 1150m, 750m);
            _profiles[114] = new BottleWeightProfileDto(114, 400m, 1150m, 750m);
        }
    }
}
