using System.ComponentModel.DataAnnotations;

namespace BOTGC.API.Dto
{
    public sealed class SaveStockTakeItem
    {
        [Required]
        public int StockItemId { get; set; }

        [Range(0, double.MaxValue)]
        public decimal Quantity { get; set; }

        public string? Reason { get; set; }
    }

}
