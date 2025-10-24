namespace BOTGC.API.Dto
{
    public sealed class TillProductLookupDto: HateoasResource
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
    }
}
