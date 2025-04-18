using BOTGC.API.Dto;

namespace BOTGC.API.Dto
{
    public class TrophyDto: HateoasResource
    {
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
