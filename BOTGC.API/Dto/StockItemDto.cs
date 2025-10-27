namespace BOTGC.API.Dto
{
    public class StockItemDto: HateoasResource
    {
        public int Id { get; set; }
        public string ExternalId { get; set; }
        public string Name { get; set; }
        public int? MinAlert { get; set; }
        public int? MaxAlert { get; set; }
        public bool? IsActive { get; set; }
        public int? TillStockDivisionId { get; set; }
        public string Unit { get; set; }
        public decimal? Quantity { get; set; }
        public int? TillStockRoomId { get; set; }
        public string Division { get; set; }
        public decimal? Value { get; set; }
        public decimal? TotalQuantity { get; set; }
        public decimal? TotalValue { get; set; }
        public decimal? OneQuantity { get; set; }
        public decimal? OneValue { get; set; }
        public decimal? TwoQuantity { get; set; }
        public decimal? TwoValue { get; set; }
    }
}
