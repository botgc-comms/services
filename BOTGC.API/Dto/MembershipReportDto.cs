namespace Services.Dto
{
    public class MembershipReportDto
    {
        public List<MembershipReportEntryDto> DataPoints { get; set; } = new();
        public string DataPointsCsv { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    }

    public class MembershipReportEntryDto
    {
        public DateTime Date { get; set; }
        public int TotalMembers { get; set; }
        public int PlayingMembers { get; set; }
        public double TargetPlayingMembers { get; set; }
        public int NonPlayingMembers { get; set; }
        public int LadyMembers { get; set; }
        public int JuniorMembers { get; set; }
        public Dictionary<string, int> CategoryBreakdown { get; set; } = new();
        public decimal TrendPercentage { get; set; }
    }
}
