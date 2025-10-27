namespace BOTGC.API.Dto
{
    public sealed class TillProductStockComponentDto
    {
        public int? StockId { get; set; }
        public string? ExternalId { get; set; }
        public string? Name { get; set; }
        public string? Unit { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? SellingPrice { get; set; }
    }
}
