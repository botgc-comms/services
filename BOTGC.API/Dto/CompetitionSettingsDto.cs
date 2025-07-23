using BOTGC.API.Dto;

namespace BOTGC.API.Dto
{
    public enum TeeColor { White, Yellow, Red }

    public record TeeKey(Gender Gender, TeeColor Color);

    public class CompetitionSettingsDto: HateoasResource
    {
        public int? Id { get; set; }
        public string[] Tags { get; set; }
        public string Comments { get; set; }
        public string Name { get; set; }
        public string Contact { get; set; }
        public string Format { get; set; }

        public string ResultsDisplay { get; set; }
        public DateTime Date { get; set; }

        public Dictionary<string, int>? MultiPartCompetition { get; set; }

        public Dictionary<string, int> CoursePar { get; set; } = new Dictionary<string, int>
        {
            ["gents.white"] = 71,
            ["gents.yellow"] = 70,
            ["gents.red"] = 70,
            ["ladies.yellow"] = 75,
            ["ladies.red"] = 73,
        };
    }

    public class CompetitionSummaryDto : HateoasResource
    {
        public int? Id { get; set; }
        public Dictionary<string, int>? MultiPartCompetition { get; set; }
    }
}
