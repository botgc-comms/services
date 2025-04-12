namespace Services.Dto
{
    public class SecurityLogEntryDto : HateoasResource
    {
        public DateTime? OccuredAt { get; set; }
        public string Event { get; set; }
        public string Admin { get; set; }
        public string Subject { get; set; }
        public string IP { get; set; }
    }
}
