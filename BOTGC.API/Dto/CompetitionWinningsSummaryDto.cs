namespace BOTGC.API.Dto
{
    public sealed class CompetitionWinningsSummaryDto
    {
        public int CompetitionId { get; set; }
        public string CompetitionName { get; set; } = string.Empty;
        public DateTime CompetitionDate { get; set; }
        public int Entrants { get; set; }
        public decimal PrizePot { get; set; }
        public decimal Revenue { get; set; }
        public string Currency { get; set; } = "GBP";
        public TopEarnerDto? TopEarner { get; set; }
        public List<DivisionResultDto> Divisions { get; set; } = new();
    }
}
