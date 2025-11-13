using Microsoft.Extensions.ObjectPool;
using BOTGC.API.Dto;

namespace BOTGC.API.Dto
{
    public class CompetitionDto : HateoasResource
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public DateTime? Date { get; set; }
        public Gender? Gender { get; set; }
        public bool AvailableForHandicaping { get; set; }
        public bool IsMultidayParent { get; set; }
        public bool IsAlternateDay { get; set; }

        public Dictionary<string, int>? MultiPartCompetition { get; set; }
    }

    public enum Gender
    {
        Unknown,
        Ladies,
        Juniors,
        Gents,
        Mixed
    }
}
