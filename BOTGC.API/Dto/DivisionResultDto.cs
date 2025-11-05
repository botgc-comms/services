namespace BOTGC.API.Dto
{
    public sealed class DivisionResultDto
    {
        public int DivisionNumber { get; set; }
        public string DivisionName { get; set; } = string.Empty;
        public List<PlacingDto> Placings { get; set; } = new();
    }
}
