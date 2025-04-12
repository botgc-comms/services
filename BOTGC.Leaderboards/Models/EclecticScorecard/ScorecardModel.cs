namespace BOTGC.Leaderboards.Models.EclecticScorecard
{
    public class ScorecardModel
    {
        public string PlayerName { get; set; }
        public int TotalStablefordScore { get; set; }
        public List<HoleModel> Holes { get; set; }
    }
}
