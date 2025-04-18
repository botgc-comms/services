namespace BOTGC.API.Dto
{
    public class PlayerIdLookupDto : HateoasResource
    {
        public int MemberId { get; set; } 
        public int PlayerId { get; set; }
    }
}
