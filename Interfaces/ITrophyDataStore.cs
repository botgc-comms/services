using Services.Models;

namespace Services.Interfaces
{
    public interface ITrophyDataStore
    {
        Task<TrophyMetadata?> GetTrophyByIdAsync(string id);
        Task<Stream?> GetWinnerImageByTrophyIdAsync(string id);
        Task<IReadOnlyCollection<string>> ListTrophyIdsAsync();
    }
}