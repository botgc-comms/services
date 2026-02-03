using BOTGC.API.Models;

namespace BOTGC.API.Interfaces;

public interface ILearningPackProgressReadStore
{
    Task<IReadOnlyList<LearningPackProgressRow>> ListCompletedPacksAsync(
        string userId,
        DateTimeOffset? sinceUtc,
        int take,
        CancellationToken cancellationToken);
}