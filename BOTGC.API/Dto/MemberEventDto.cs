namespace BOTGC.API.Dto
{
    public class MemberEventDto : HateoasResource
    {
        public int ChangeIndex { get; set; }
        public int MemberId { get; set; } 
        public DateTime? DateOfChange { get; set; }
        public DateTime? DateCreated { get; set; }
        public string FromCategory { get; set; }
        public string ToCategory { get; set; } 
        public string FromStatus { get; set; }
        public string ToStatus { get; set; } 
    }
}
