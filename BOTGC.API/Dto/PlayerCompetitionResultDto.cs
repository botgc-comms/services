namespace BOTGC.API.Dto
{
    public class PlayerCompetitionResultDto
    {
        public required CompetitionSettingsDto CompetitionDetails { get; set; } 
        public required LeaderboardPlayerDto? LeaderBoardEntry { get; set; } 
    }
}
