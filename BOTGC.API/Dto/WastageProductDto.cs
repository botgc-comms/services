namespace BOTGC.API.Dto
{
    public class WastageProductDto : HateoasResource
    {
        public int Id { get; set; }
        public string Name { get; set; }
        
        public List<StockItemDto> StockItems { get; set; }
    }
}
