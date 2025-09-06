namespace BOTGC.API.Dto
{
    public class PlayerHandicapSummaryDto : HateoasResource
    {
        public MemberDetailsDto MemberDetails { get; set; }

        public DateTime FirstHandicapDate { get; set; }
        public double ChangeYTD { get; set; }
        public double Change1Year { get; set; }
        public double CurrentHandicap { get; set; }
        public double FirstHandicap { get; set; }

        public List<HandicapIndexPointDto> HandicapIndexPoints { get; set; }
    }
}
