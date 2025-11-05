namespace BOTGC.API.Dto
{
    public sealed class TopEarnerDto
    {
        public string CompetitorId { get; set; } = string.Empty;
        public string CompetitorName { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public int Wins { get; set; }
        public int Placings { get; set; }
    }
}
