namespace BOTGC.Leaderboards.Models.EclecticScorecard
{
    public class ExcludedRound
    {
        public string MemberId { get; set; }
        public string RoundId { get; set; }
        public string Type { get; set; }
        public DateTime DatePlayed { get; set; }
        public string ExclusionReason { get; set; }
    }
}
