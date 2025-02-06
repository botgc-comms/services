namespace Services.Dto
{
    public class MemberDto : HateoasResource
    {
        public int MemberId { get; set; } 
        public string Title { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string FullName { get; set; }
        public string Gender { get; set; }
        public string MembershipCategory { get; set; }
        public string MembershipStatus { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string Town { get; set; }
        public string County { get; set; }
        public string Postcode { get; set; }
        public string Email { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime? JoinDate { get; set; }
        public DateTime? LeaveDate { get; set; }
        public string Handicap { get; set; }
        public bool IsDisabledGolfer { get; set; }
        public decimal UnpaidTotal { get; set; }
        public bool IsActive { get; set; } 
    }
}
