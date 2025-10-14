using BOTGC.API.Dto;

namespace BOTGC.API.Interfaces
{
    public interface IBottleWeightDataSource
    {
        Task<BottleWeightProfileDto?> GetAsync(int stockItemId, CancellationToken ct);
    }
}