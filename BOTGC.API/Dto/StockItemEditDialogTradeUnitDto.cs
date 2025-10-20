namespace BOTGC.API.Dto
{
    /// <summary>
    /// A Trade Unit row from the “Trade Units” table.
    /// </summary>
    public class StockItemEditDialogTradeUnitDto
    {
        /// <summary>The Trade Unit internal identifier.</summary>
        public int Id { get; set; }
        /// <summary>The Trade Unit display name.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>The unit cost (currency parsed in invariant culture).</summary>
        public decimal Cost { get; set; }
        /// <summary>The conversion ratio to the product’s Base Unit.</summary>
        public decimal ConversionRatio { get; set; }
        /// <summary>The precision for quantities of this unit.</summary>
        public int PrecisionOfUnit { get; set; }
    }
}
