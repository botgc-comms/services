namespace BOTGC.Leaderboards.Models.EclecticScorecard
{
    public class ScoreEntry
    {
        public ScorecardModel Scorecard { get; set; }
        public List<ExcludedRound> ExcludedRounds { get; set; }
    }
}
