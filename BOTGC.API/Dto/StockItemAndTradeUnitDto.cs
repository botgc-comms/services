namespace BOTGC.API.Dto
{
    public class StockItemAndTradeUnitDto : HateoasResource
    {
        public int Id { get; set; }
        public string? ExternalId { get; set; }
        public string Name { get; set; }
        public bool? IsActive { get; set; }
        public string Unit { get; set; }
        public string Division { get; set; }
        
        public List<StockItemUnitInfoDto> TradeUnits { get; set; }
    }

}
