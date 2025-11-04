namespace BOTGC.API.Dto
{
    public class EclecticRoundExclusionReasonDto
    {
        public required string MemberId { get; set; }
        public required string RoundId { get; set; }
        public required string Type { get; set; }
        public required DateTime DatePlayed { get; set; }
        public required string ExclusionReason { get; set; }
    }
}
