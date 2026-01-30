using BOTGC.API.Dto;

public class MemberWithoutHandicapDto: HateoasResource
{
    public MemberWithoutHandicapDto() { }

    public int PlayerId { get; set; }
    public string PlayerName { get; set; } = string.Empty;
}
