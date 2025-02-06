using Services.Dto;

namespace Services.Dtos
{
    public class TrophyDto: HateoasResource
    {
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
