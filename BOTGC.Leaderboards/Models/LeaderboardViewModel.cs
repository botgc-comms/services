namespace BOTGC.Leaderboards.Models
{
    public class LeaderboardViewModel
    {
        public List<PlayerViewModel> Players { get; set; }
        public CompetitionDetailsViewModel CompetitionDetails { get; set; }
    }

    public class CompetitionDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Format { get; set; }
        public string ResultsDisplay { get; set; }
        public DateTime Date { get; set; }
    }

    public class LeaderboardResultsRoot
    {
        public List<LeaderboardPlayer> Players { get; set; }
        public CompetitionSettings CompetitionDetails { get; set; }
    }

    public class LeaderboardPlayer
    {
        public int? Position { get; set; }
        public string PlayerName { get; set; }
        public string PlayingHandicap { get; set; }
        public int? PlayerId { get; set; }
        public string? StablefordScore { get; set; }
        public string? NetScore { get; set; }
        public string Countback { get; set; }
        public string Thru { get; set; }
    }

    public class CompetitionSettings
    {
        public int? Id { get; set; }
        public string[] Tags { get; set; }
        public string Comments { get; set; }
        public string Name { get; set; }
        public string Contact { get; set; }
        public string Format { get; set; }

        public string ResultsDisplay { get; set; }
        public DateTime Date { get; set; }
    }

}
