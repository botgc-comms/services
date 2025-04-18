using BOTGC.API.Interfaces;
using BOTGC.API.Models;
using BOTGC.API.Services.CompetitionProcessors;
using System.Threading.Channels;

namespace BOTGC.API.Common
{
    public class CompetitionProcessorResolver : ICompetitionProcessorResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public CompetitionProcessorResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public ICompetitionProcessor GetProcessor(string competitionType)
        {
            // Based on competition type, resolve the appropriate processor
            return competitionType switch
            {
                "JuniorEclectic" => _serviceProvider.GetService<JuniorEclecticCompetitionProcessor>(),
                _ => throw new ArgumentException("Unknown competition type", nameof(competitionType))
            };
        }
    }
}
