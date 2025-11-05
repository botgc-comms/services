namespace BOTGC.API.Dto
{
    public sealed class ManualCompetitionResultDto
    {
        public DateTime Date { get; set; }
        public string CompetitionName { get; set; } = string.Empty;
        public int? Entrants { get; set; }
        public decimal PrizePot { get; set; }
        public string? Url { get; set; }
    }
}
