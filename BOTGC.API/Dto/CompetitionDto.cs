using Microsoft.Extensions.ObjectPool;
using Services.Dto;

namespace Services.Dtos
{
    public class CompetitionDto: HateoasResource
    {
        public int? Id { get; set; }
        public string Name { get; set; } 
        public DateTime? Date { get; set; }
        public Gender? Gender { get; set; }
        public bool AvailableForHandicaping { get; set; }
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
