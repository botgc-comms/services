using BOTGC.API.Dto;

namespace BOTGC.API.Dto
{
    public class LeaderBoardDto : HateoasResource
    {
        public List<LeaderboardPlayerDto> Players { get; set; }
        public CompetitionSettingsDto CompetitionDetails { get; set; }
    }

    public class LeaderboardPlayerDto
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
}
