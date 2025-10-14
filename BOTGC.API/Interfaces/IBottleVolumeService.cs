namespace BOTGC.API.Interfaces
{
    public interface IBottleVolumeService
    {
        Task<decimal> ToVolumeMlAsync(int stockItemId, decimal observedWeightGrams, CancellationToken ct);
    }
}