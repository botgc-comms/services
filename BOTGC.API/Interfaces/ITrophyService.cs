using BOTGC.API.Models;

namespace BOTGC.API.Interfaces
{
    public interface ITrophyService
    {
        Task<TrophyMetadata?> GetTrophyByIdAsync(string id);
        Task<Stream?> GetWinnerImageByTrophyIdAsync(string id);
        Task<IReadOnlyCollection<string>> ListTrophyIdsAsync();
    }
}