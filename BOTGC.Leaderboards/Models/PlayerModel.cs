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

    public class PlayerMultiRoundViewModel
    {
        public string Name { get; set; }
        public string Par { get; set; }
        public int Thru { get; set; }
        public int Position { get; set; }
        public string R1 { get; set; } 
        public string R2 { get; set; } 
    }
}
