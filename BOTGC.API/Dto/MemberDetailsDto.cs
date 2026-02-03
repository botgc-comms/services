namespace BOTGC.API.Dto
{
    public class MemberDetailsDto : HateoasResource
    {
        public int ID { get; set; }
        public int MemberNumber { get; set; }
        public string Forename { get; set; }
        public string Surname { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string MembershipStatus { get; set; }
        public string MembershipCategory { get; set; }
        public MembershipPrimaryCategories PrimaryCategory { get; set; }
        public DateTime? JoinDate { get; set; }
        public DateTime? LeaveDate { get; set; }
        public DateTime? ApplicationDate { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? Address3 { get; set; }
        public string? Town { get; set; }
        public string? County { get; set; }
        public string? Postcode { get; set; }
        public string? Email { get; set; }
        public Dictionary<string, List<string>> FurtherInformation { get; set; }
    }
}
