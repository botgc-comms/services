namespace BOTGC.API.Dto
{
    /// <summary>
    /// Product and Trade Unit data captured from the IG “Edit Product” page.
    /// </summary>
    public class StockItemEditDialogDto: HateoasResource
    {
        /// <summary>The IG internal item identifier.</summary>
        public int Id { get; set; }
        /// <summary>The product name as displayed in the Name input.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>The selected Base Unit identifier (e.g., 3 = CAN).</summary>
        public int? BaseUnitId { get; set; }
        /// <summary>The selected Division identifier (e.g., 7 = BEER CANS).</summary>
        public int? DivisionId { get; set; }
        /// <summary>The minimum alert threshold, if provided.</summary>
        public decimal? MinAlert { get; set; }
        /// <summary>The maximum alert threshold, if provided.</summary>
        public decimal? MaxAlert { get; set; }
        /// <summary>Whether the item is currently marked Active.</summary>
        public bool Active { get; set; }
        /// <summary>All configured Trade Units for the item, in table order.</summary>
        public List<StockItemEditDialogTradeUnitDto> TradeUnits { get; set; } = new();
    }
}
