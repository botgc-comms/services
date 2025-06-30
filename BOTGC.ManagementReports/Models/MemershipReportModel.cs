namespace BOTGC.ManagementReports.Models;
public enum MembershipAnomalyType
{
    StatusRWithPastLeaveDate,
    StatusSWithPastLeaveDate,
    StatusLWithoutLeaveDate,
    StatusWithoutJoinDate
}

public class MembershipReportDto
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
    public Dictionary<string, int> PlayingCategoryBreakdown { get; set; } = new();
    public Dictionary<string, int> NonPlayingCategoryBreakdown { get; set; } = new();
    public Dictionary<string, int> CategoryGroupBreakdown { get; set; } = new();
    public Dictionary<string, int> DailyJoinersByCategoryGroup { get; set; } = new();
    public Dictionary<string, int> DailyLeaversByCategoryGroup { get; set; } = new();
    public Dictionary<string, int> CategoryGroupTotals { get; set; } = new();

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

}

public class MembershipSnapshotDto
{
    public DateTime SnapshotDate { get; set; }
    public List<MemberDto> Members { get; set; } = new();
}

public enum MembershipPrimaryCategories
{
    None,
    PlayingMember,
    NonPlayingMember
}

public class MemberDto
{
    public int? PlayerId { get; set; }
    public int? MemberNumber { get; set; }
    public string? Title { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? Gender { get; set; }
    public string? MembershipCategory { get; set; }
    public string? MembershipStatus { get; set; }
    public MembershipPrimaryCategories PrimaryCategory { get; set; } = MembershipPrimaryCategories.None;
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? Address3 { get; set; }
    public string? Town { get; set; }
    public string? County { get; set; }
    public string? Postcode { get; set; }
    public string? Email { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public DateTime? JoinDate { get; set; }
    public DateTime? LeaveDate { get; set; }
    public string? Handicap { get; set; }
    public bool? IsDisabledGolfer { get; set; }
    public decimal? UnpaidTotal { get; set; }
    public bool? IsActive { get; set; }

    public MemberDto() { }

    public MemberDto(MemberDto source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        PlayerId = source.PlayerId;
        MemberNumber = source.MemberNumber;
        Title = source.Title;
        FirstName = source.FirstName;
        LastName = source.LastName;
        FullName = source.FullName;
        Gender = source.Gender;
        MembershipCategory = source.MembershipCategory;
        MembershipStatus = source.MembershipStatus;
        PrimaryCategory = source.PrimaryCategory;
        Address1 = source.Address1;
        Address2 = source.Address2;
        Address3 = source.Address3;
        Town = source.Town;
        County = source.County;
        Postcode = source.Postcode;
        Email = source.Email;
        DateOfBirth = source.DateOfBirth;
        JoinDate = source.JoinDate;
        LeaveDate = source.LeaveDate;
        Handicap = source.Handicap;
        IsDisabledGolfer = source.IsDisabledGolfer;
        UnpaidTotal = source.UnpaidTotal;
        IsActive = source.IsActive;
    }
}

public class MemberSummmaryDto
{
    public MemberSummmaryDto() { }

    public MemberSummmaryDto(MemberDto member)
    {
        this.MemberId = member.MemberNumber!.Value;
        this.FullName = member.FullName!;
        this.MembershipStatus = member.MembershipStatus!;
        this.MembershipCategory = member.MembershipCategory!;
    }

    public int MemberId { get; set; }
    public string FullName { get; set; }
    public string MembershipCategory { get; set; }
    public string MembershipStatus { get; set; }
}


