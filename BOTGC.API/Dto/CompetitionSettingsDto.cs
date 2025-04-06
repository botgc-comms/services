using Services.Dto;

namespace Services.Dtos
{
    public class CompetitionSettingsDto: HateoasResource
    {
        public int? Id { get; set; }
        public string[] Tags { get; set; }
        public string Comments { get; set; }
        public string Name { get; set; }
        public string Contact { get; set; }
        public string Format { get; set; }

        public string ResultsDisplay { get; set; }
        public DateTime Datae { get; set; }
    }
}
