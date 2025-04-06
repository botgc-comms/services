namespace BOTGC.API.Dto
{
    public class NewMemberApplicationDto
    {
        public int Gender { get; set; }
        public string Title { get; set; }
        public string Forename { get; set; }
        public string Surname { get; set; }
        public string AltForename { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Telephone1 { get; set; }
        public string Telephone2 { get; set; }
        public string Telephone3 { get; set; }
        public string Email { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string AddressLine3 { get; set; }
        public string Town { get; set; }
        public string County { get; set; }
        public string Country { get; set; }
        public string Postcode { get; set; }
        public bool HasCdhId { get; set; }
        public string CdhIdLookup { get; set; }
        public int MemberCategory { get; set; }
        public int PaymentType { get; set; }
        public string MemberStatus { get; set; }
        public int NewMemberId { get; set; }
        public string NewCardSwipe { get; set; }
        public DateTime JoinDate { get; set; }
        public DateTime ApplicationDate { get; set; }
        public Dictionary<int, bool> ContactPreferences { get; set; } = new();
        public int TillDiscountGroupId { get; set; }
    }

    public class NewMemberApplicationResultDto
    {
        public int MemberId { get; set; }
    }
}
