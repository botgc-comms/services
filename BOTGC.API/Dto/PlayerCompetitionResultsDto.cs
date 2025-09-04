namespace BOTGC.API.Dto
{
    public class PlayerCompetitionResultsDto : HateoasResource
    {
        public string? MemberId { get; set; }
        public int? PlayerId { get; set; }
        public string PlayerName { get; set; }

        public List<PlayerCompetitionResultDto> CompetitionResults { get; set; }    
    }
}
