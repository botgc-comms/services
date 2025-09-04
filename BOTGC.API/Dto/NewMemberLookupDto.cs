namespace BOTGC.API.Dto
{
    public class NewMemberLookupDto: HateoasResource
    {
        public int MemberNumber { get; set; }
        public string Forename { get; set; }
        public string Surname { get; set; }
        public DateTime? JoinDate { get; set; }
    }
}
