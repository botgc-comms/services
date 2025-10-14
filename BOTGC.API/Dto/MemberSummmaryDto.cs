namespace BOTGC.API.Dto
{
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
