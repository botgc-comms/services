using Services.Models;

namespace Services.Interfaces
{
    public interface ICompetitionProcessor
    {
        Task ProcessCompetitionAsync(DateTime fromDate, DateTime toDate, CancellationToken cancellationToken);
    }

}