using Services.Models;

namespace Services.Interfaces
{
    public interface ITrophyFiles
    {
        Task<TrophyMetadata?> GetTrophyByIdAsync(string id);
        Task<Stream?> GetWinnerImageByTrophyIdAsync(string id);
        Task<IReadOnlyCollection<TrophyMetadata>> ListTrophiesAsync();
    }
}