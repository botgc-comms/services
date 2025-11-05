namespace BOTGC.API.Dto
{
    public sealed class YearlyWinningsSummaryDto
    {
        public int Year { get; set; }
        public decimal TotalPaid { get; set; }
        public int CompetitionsCount { get; set; }
        public TopEarnerDto[] OverallTop3 { get; set; } = Array.Empty<TopEarnerDto>();
        public List<CompetitionWinningsSummaryDto> Competitions { get; set; } = new();
    }
}
