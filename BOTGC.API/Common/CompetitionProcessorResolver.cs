using Services.Interfaces;
using Services.Models;
using Services.Services.CompetitionProcessors;
using System.Threading.Channels;

namespace Services.Common
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
