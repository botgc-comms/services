namespace BOTGC.API.Dto
{
    public sealed class PlacingDto
    {
        public int Position { get; set; }
        public string CompetitorId { get; set; } = string.Empty;
        public string CompetitorName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
