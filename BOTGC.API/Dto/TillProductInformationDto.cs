namespace BOTGC.API.Dto
{
    public sealed class TillProductInformationDto: HateoasResource
    {
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public decimal? DiscountClubhouseSocial { get; set; }
        public decimal? DiscountStaff { get; set; }
        public decimal? DiscountStandard { get; set; }
        public decimal? SellingPriceIncVat { get; set; }
        public List<TillProductStockComponentDto> Components { get; set; } = new();
    }
}
