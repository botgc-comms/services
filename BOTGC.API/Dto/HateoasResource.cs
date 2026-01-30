namespace BOTGC.API.Dto;


public class HateoasResource
{
    public List<HateoasLink> Links { get; set; } = new();
}

public class HateoasLink
{
    public string Rel { get; set; } = string.Empty;   // Relation (e.g., self, next, previous)
    public string Href { get; set; } = string.Empty;  // Link URL
    public string Method { get; set; } = "GET";       // HTTP Method (GET, POST, PUT, DELETE)
}
