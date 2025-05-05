namespace BOTGC.API.Dto
{
    public class MemberCDHLookupDto : HateoasResource
    {
        public string? CdhId { get; set; }
        public string? HandicapIndex { get; set; }
        public string? WhsMemberUid { get; set; }
        public string? ClubName { get; set; }
        public string? ClubUid { get; set; }
    }
}
