namespace BOTGC.API.Dto
{
    public class NewMemberApplicationDto
    {
        public int Gender { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Forename { get; set; } = string.Empty;
        public string Surname { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }

        public string Telephone { get; set; } = string.Empty;
        public string? AlternativeTelephone { get; set; }
        public string Email { get; set; } = string.Empty;

        public string AddressLine1 { get; set; } = string.Empty;
        public string AddressLine2 { get; set; } = string.Empty;
        public string Town { get; set; } = string.Empty;
        public string County { get; set; } = string.Empty;
        public string Postcode { get; set; } = string.Empty;

        public bool HasCdhId { get; set; }
        public string? CdhId { get; set; }

        public string MembershipCategory { get; set; } = string.Empty;
        public string PaymentType { get; set; } = string.Empty;

        public Dictionary<string, bool> ContactPreferences { get; set; } = new();

        public bool AgreeToClubRules { get; set; }

        // Internal fields
        public string MemberStatus { get; set; } = "Pending";
        public DateTime ApplicationDate { get; set; } = DateTime.UtcNow;
    }


    public class NewMemberApplicationResultDto
    {
        public int MemberId { get; set; }
    }
}
