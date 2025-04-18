using BOTGC.API.Models;

namespace BOTGC.API.Interfaces
{
    public interface ICompetitionProcessorResolver
    {
        ICompetitionProcessor GetProcessor(string competitionType);
    }
}