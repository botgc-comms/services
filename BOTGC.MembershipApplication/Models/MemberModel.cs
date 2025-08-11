namespace BOTGC.MembershipApplication.Models
{
    public enum MembershipPrimaryCategories
    {
        None,
        PlayingMember,
        NonPlayingMember
    }


    public class MemberModel
    {
        public int? MemberNumber { get; set; }
        public string? Title { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public string? Gender { get; set; }
        public string? MembershipCategory { get; set; }
        public string? MembershipCategoryGroup { get; set; }
        public string? MembershipStatus { get; set; }
        public MembershipPrimaryCategories PrimaryCategory { get; set; } = MembershipPrimaryCategories.None;
        public string? Postcode { get; set; }
        public string? Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? JoinDate { get; set; }
        public DateTime? LeaveDate { get; set; }
        public DateTime? ApplicationDate { get; set; }
    }
}
