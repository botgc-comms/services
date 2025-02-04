namespace Services.Dtos
{
    public class TrophyDto
    {
        public string Slug { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Hypermedia links following HATEOAS principles.
        /// </summary>
        public Dictionary<string, string> Links { get; set; } = new();
    }
}
