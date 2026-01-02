namespace BOTGC.API.Dto
{
    public enum MembershipAnomalyType
    {
        StatusRWithPastLeaveDate,
        StatusSWithPastLeaveDate,
        StatusLWithoutLeaveDate,
        StatusWithoutJoinDate
    }

    public class MembershipReportDto: HateoasResource
    {
        public List<MembershipReportEntryDto> DataPoints { get; set; } = new();
        public string DataPointsCsv { get; set; }
        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
        public List<MembershipAnomalyDto> Anomalies { get; set; } = new();

        public List<MembershipDeltaDto> QuarterlyStats { get; set; } = new();
        public List<MembershipDeltaDto> MonthlyStats { get; set; } = new();

        public DateTime Today { get; set; }
        public DateTime SubscriptionYearStart { get; set; }
        public DateTime SubscriptionYearEnd { get; set; }
        public DateTime FinancialYearStart { get; set; }
        public DateTime FinancialYearEnd { get; set; }

        public LostRevenueBreakdownDto? LostRevenueAprToSep { get; set; }

    }

    public sealed class LostRevenueBreakdownDto
    {
        public decimal Total { get; set; }
        public Dictionary<string, decimal> ByMonth { get; set; } = new(StringComparer.Ordinal);
    }

    public class MembershipReportEntryDto
    {
        public DateTime Date { get; set; }
        public int TotalMembers { get; set; }
        public int PlayingMembers { get; set; }
        public double AveragePlayingMembersAge { get; set; }
        public double TargetPlayingMembers { get; set; }
        public int NonPlayingMembers { get; set; }
        public int LadyMembers { get; set; }
        public int JuniorMembers { get; set; }
        public decimal ActualRevenue { get; set; }
        public decimal TargetRevenue { get; set; }
        public decimal ReceivedRevenue { get; set; }
        public decimal BilledRevenue { get; set; }
        public Dictionary<string, int> PlayingCategoryBreakdown { get; set; } = new();
        public Dictionary<string, int> NonPlayingCategoryBreakdown { get; set; } = new();
        public Dictionary<string, int> CategoryGroupBreakdown { get; set; } = new();
        public Dictionary<string, int> DailyJoinersByCategoryGroup { get; set; } = new();
        public Dictionary<string, int> DailyLeaversByCategoryGroup { get; set; } = new();
        public Dictionary<string, int> WaitingListCategoryBreakdown { get; set; } = new();

        public Dictionary<string, int>? WaitingListApplicationsByGroup { get; set; }
        public Dictionary<string, int>? WaitingListConversionsByAppliedGroup { get; set; }
        public Dictionary<string, int>? WaitingListDropoutsByAppliedGroup { get; set; }

        public int WaitingListApplicationsTotal { get; set; }
        public int WaitingListConversionsTotal { get; set; }
        public int WaitingListDropoutsTotal { get; set; }

        public decimal TrendPercentage { get; set; }
    }

    public class MembershipAnomalyDto
    {
        public DateTime DetectedDate { get; set; }
        public MembershipAnomalyType Type { get; set; }
        public string Description { get; set; }
        public List<MemberSummmaryDto> Members { get; set; }
    }

    public class MembershipDeltaDto
    {
        public DateTime FromDate { get; set; } 
        public DateTime ToDate { get; set; }   
        public string PeriodDescription { get; set; } 

        public int NewMembers { get; set; }  
        public int Leavers { get; set; }     
        public int Deaths { get; set; }      

        public Dictionary<string, int> CategoryChanges { get; set; } = new();
        public Dictionary<string, int> CategoryGroupTotals { get; set; } = new();

        public int WaitingListApplications { get; set; }
        public int WaitingListConversions { get; set; }
        public int WaitingListDropouts { get; set; }

        public Dictionary<string, int>? WaitingListApplicationsByGroup { get; set; }
        public Dictionary<string, int>? WaitingListConversionsByAppliedGroup { get; set; }
        public Dictionary<string, int>? WaitingListDropoutsByAppliedGroup { get; set; }
    }


    public class MembershipSnapshotDto
    {
        public DateTime SnapshotDate { get; set; }
        public List<MemberDto> Members { get; set; } = new();
    }

}
