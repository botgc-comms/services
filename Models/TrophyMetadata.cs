namespace Services.Models
{
    public class TrophyMetadata
    {
        public string Slug { get; set; } = string.Empty; 
        public string Name { get; set; } = string.Empty; 
        public string Description { get; set; } = string.Empty; 
        public string? ImageUrl { get; set; } 
        public string? WinnerImage { get; set; } 
    }
}
