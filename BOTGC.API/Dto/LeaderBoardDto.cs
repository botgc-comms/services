using BOTGC.API.Dto;

namespace BOTGC.API.Dto
{
    public class LeaderBoardDto : HateoasResource
    {
        public List<LeaderboardPlayerDto> Players { get; set; }
        public CompetitionSettingsDto CompetitionDetails { get; set; }

        public List<DivisionDto> Divisions { get; set; }
    }

    public class DivisionDto
    {
        public int Number { get; set; }
        public int? Limit { get; set;  }
        public List<LeaderboardPlayerDto> Players { get; set; } 
    }

    public class PlayerDtoBase: HateoasResource
    {
        public int? Position { get; set; }
        public string PlayerName { get; set; }
        public int? PlayerId { get; set; }
    }

    public class LeaderboardPlayerDto: PlayerDtoBase
    {
        public string PlayingHandicap { get; set; }
        public string? StablefordScore { get; set; }
        public string? NetScore { get; set; }
        public string Countback { get; set; }
        public string Thru { get; set; }
    }

    public class ClubChampionshipLeaderBoardDto : HateoasResource
    {
        public CompetitionSettingsDto CompetitionDetails { get; set; }
        public List<ChampionshipLeaderboardPlayerDto> Round1 { get; set; }
        public List<ChampionshipLeaderboardPlayerDto> Round2 { get; set; }
        public List<ChampionshipLeaderboardPlayerDto> Total { get; set; }
    }

    public class ChampionshipLeaderboardPlayerDto : PlayerDtoBase
    {
        public string Thru { get; set; }
        public string Par { get; set; }
        public string R1 { get; set; }
        public string R2 { get; set; }
        public string Score { get; set; }
        public string Countback { get; set; }
    }
}
