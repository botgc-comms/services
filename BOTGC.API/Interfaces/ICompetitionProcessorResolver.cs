using Services.Models;

namespace Services.Interfaces
{
    public interface ICompetitionProcessorResolver
    {
        ICompetitionProcessor GetProcessor(string competitionType);
    }
}