namespace BOTGC.API.Dto
{
    public enum MembershipPrimaryCategories
    {
        None, 
        PlayingMember, 
        NonPlayingMember
    }

    public class MemberDto : HateoasResource
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
}
