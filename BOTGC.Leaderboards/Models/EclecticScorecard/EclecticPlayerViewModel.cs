namespace BOTGC.Leaderboards.Models.EclecticScorecard
{
    public class EclecticPlayerViewModel
    {
        public string PlayerName { get; set; }
        public int BestFrontNine { get; set; }
        public int FrontNineCards { get; set; }
        public int BestBackNine { get; set; }
        public int BackNineCards { get; set; }
        public int TotalScore { get; set; }
        public int TotalScoreCountBack { get; set; }
        public ScoreBreakdownViewModel Scores { get; set; }
    }

    public class ScoreBreakdownViewModel
    {
        public List<ScoreCardViewModel> ScoreCards { get; set; }
    }

    public class ScoreCardViewModel
    {
        public DateTime PlayedOn { get; set; }
        public bool IsIncluded { get; set; }
        public List<HoleViewModel> Holes { get; set; }
        public List<string> ExclusionReasons { get; set; }
    }

    public class HoleViewModel
    {
        public int HoleNumber { get; set; }
        public int? HoleScore { get; set; }
        public bool IsSelected { get; set; }
    }
}
