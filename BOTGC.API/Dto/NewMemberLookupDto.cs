namespace BOTGC.API.Dto
{
    public class NewMemberLookupDto: HateoasResource
    {
        public int PlayerId { get; set; }
        public string Forename { get; set; }
        public string Surname { get; set; }
        public DateTime? JoinDate { get; set; }
    }
}
