namespace BOTGC.API.Dto;

public class ParentChildDto: HateoasResource
{
    public int ParentMemberId { get; set; }
    public IReadOnlyList<int> Children { get; set; }
}