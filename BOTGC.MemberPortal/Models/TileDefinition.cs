namespace BOTGC.MemberPortal.Models;

public sealed class TileDefinition
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Color { get; set; } = "#ffffff";
    public int SortOrder { get; set; }
    public List<string> Categories { get; set; } = new List<string>();
}
