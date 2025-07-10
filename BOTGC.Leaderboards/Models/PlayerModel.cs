namespace BOTGC.Leaderboards.Models
{
    public class PlayerViewModel
    {
        public int Position { get; set; }
        public string Names { get; set; }
        public string PlayingHandicap { get; set; }
        public string? Score { get; set; }
        public string Countback { get; set; }
    }

    public class PlayerMultiRound
    {
        public string Name { get; set; }
        public int Total { get; set; }
        public int Thru { get; set; }
        public int Position { get; set; }
        public int Ph { get; set; } // Handicap
        public string Final { get; set; }
        public int R1 { get; set; } // Round 1 Score
        public int R2 { get; set; } // Round 2 Score
    }
}
