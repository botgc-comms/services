namespace Services.Models
{
    public class RoundExclusionReason
    {
        public required string MemberId { get; set; }
        public required string RoundId { get; set; } 
        public required DateTime DatePlayed { get; set; }
        public required string ExclusionReason { get; set; }
    }

    public class HoleExclusionReason
    {
        public required string MemberId { get; set; }
        public required string RoundId { get; set; }
        public required string FrontBack { get; set; }
        public required DateTime DatePlayed { get; set; }
        public required string ExclusionReason { get; set; }
    }

}
