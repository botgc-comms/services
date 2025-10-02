namespace BOTGC.API.Dto
{
    public class TillOperatorDto: HateoasResource
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
    }

}
