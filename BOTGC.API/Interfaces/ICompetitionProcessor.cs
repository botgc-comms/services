using BOTGC.API.Models;

namespace BOTGC.API.Interfaces
{
    public interface ICompetitionProcessor
    {
        Task ProcessCompetitionAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken);
    }

}