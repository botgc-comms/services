using Services.Dto;

namespace Services.Dtos
{
    public class LeaderBoardDto : HateoasResource
    {
        public List<LeaderboardPlayerDto> Players { get; set; }
    }

    public class LeaderboardPlayerDto
    {
        public int? Position { get; set; }
        public string PlayerName { get; set; }
        public string PlayingHandicap { get; set; }
        public int? PlayerId { get; set; }
        public string StablefordScore { get; set; }
        public string Countback { get; set; }
        public string Thru { get; set; }
    }
}
