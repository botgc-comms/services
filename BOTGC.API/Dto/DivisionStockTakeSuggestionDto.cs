namespace BOTGC.API.Dto
{
    public class DivisionStockTakeSuggestionDto: HateoasResource
    {
        public string Division { get; set; } = string.Empty;
        public List<StockTakeSummaryDto> Products { get; set; } = new();
    }

}
